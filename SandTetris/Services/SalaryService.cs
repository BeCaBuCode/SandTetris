using System;
using SandTetris.Entities;
using SandTetris.Interfaces;
using Microsoft.EntityFrameworkCore;


namespace SandTetris.Services;

public class SalaryService(ICheckInRepository checkInRepository,
    ISalaryDetailRepository salaryDetailRepository, 
    DatabaseService databaseService) : ISalaryService
{
    public async Task<int> CalculateSalaryForEmployeeAsync(string employeeId, int month, int year)
    {
        // Fetch existing SalaryDetail
        var existingSalaryDetail = await salaryDetailRepository.GetSalaryDetailAsync(employeeId, month, year);

        // Determine BaseSalary
        if (existingSalaryDetail == null)
        {
            existingSalaryDetail = new SalaryDetail
            {
                BaseSalary = 0,
                DaysAbsent = 0,
                DaysOnLeave = 0,
                EmployeeId = employeeId,
                Month = month,
                Year = year
            };

            await salaryDetailRepository.AddSalaryDetailAsync (existingSalaryDetail);
        }

        int baseSalary = existingSalaryDetail.BaseSalary;

        // Fetch check-ins for the employee
        var checkIns = await checkInRepository.GetCheckInsForEmployeeAsync(employeeId, month, year);
        int daysAbsent = checkIns.Count(ci => ci.Status == CheckInStatus.Absent);
        int daysOnLeave = checkIns.Count(ci => ci.Status == CheckInStatus.OnLeave);

        int finalSalary = baseSalary;

        // Apply business rules
        if (daysAbsent + daysOnLeave > 10)
        {
            finalSalary = 0;
        }
        else
        {
            finalSalary -= (int)(baseSalary * 0.05 * daysAbsent + baseSalary * 0.03 * daysOnLeave);
        }

        if (existingSalaryDetail != null)
        {
            // Update existing SalaryDetail
            existingSalaryDetail.DaysAbsent = daysAbsent;
            existingSalaryDetail.DaysOnLeave = daysOnLeave;
            existingSalaryDetail.FinalSalary = finalSalary;
            existingSalaryDetail.BaseSalary = baseSalary; // Update in case it has changed
            await salaryDetailRepository.UpdateSalaryDetailAsync(existingSalaryDetail);
        }
        else
        {
            existingSalaryDetail = new SalaryDetail
            {
                EmployeeId = employeeId,
                Month = month,
                Year = year,
                BaseSalary = baseSalary,
                DaysAbsent = daysAbsent,
                DaysOnLeave = daysOnLeave,
                FinalSalary = finalSalary
            };

            await salaryDetailRepository.AddSalaryDetailAsync(existingSalaryDetail);
        }

        await CalculateSalariesAsync(existingSalaryDetail.Employee.DepartmentId, month, year);

        return finalSalary;
    }

    public async Task CalculateSalariesAsync(string departmentId, int month, int year)
    {
        await databaseService.DataContext.SalaryDetails
            .Where(ss => ss.Employee.DepartmentId == departmentId && ss.Year == year && ss.Month == month)
            .GroupBy(ss => new { ss.Employee.DepartmentId, ss.Month, ss.Year })
            .Select(g => new SalaryDetailSummary
            {
                Month = g.Key.Month,
                Year = g.Key.Year,
                DepartmentId = g.Key.DepartmentId,
                TotalSpent = g.Sum(ss => ss.FinalSalary)
            })
            .FirstOrDefaultAsync();
    }

    public async Task<int> GetDepartmentSalaryAsync(string departmentId, int month, int year)
    {
        var salaryDetails = await salaryDetailRepository.GetSalaryDetailsAsync();
        var departmentSalary = salaryDetails
            .Where(sd => sd.Employee.DepartmentId == departmentId && sd.Month == month && sd.Year == year)
            .Sum(sd => sd.FinalSalary);
        return departmentSalary;
    }

    public async Task<int> GetTotalSalaryAsync(int month, int year)
    {
        var salaryDetails = await salaryDetailRepository.GetSalaryDetailsAsync();
        var departmentSalary = salaryDetails
            .Where(sd => sd.Month == month && sd.Year == year)
            .Sum(sd => sd.FinalSalary);
        return departmentSalary;
    }

    public async Task<long> GetTotalAll()
    {
        var salaryDetails = await salaryDetailRepository.GetSalaryDetailsAsync();
        return salaryDetails.Sum(sd => (long)sd.FinalSalary);
    }

    public async Task<int> GetEmployeeSalaryAsync(string employeeId, int month, int year)
    {
        var salaryDetail = await salaryDetailRepository.GetSalaryDetailAsync(employeeId, month, year);
        return salaryDetail?.FinalSalary ?? 0;
    }
}

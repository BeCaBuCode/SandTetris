﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SandTetris.Entities;
using SandTetris.Interfaces;
using SandTetris.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SandTetris.ViewModels;

public partial class AddDepartmentPageViewModel : ObservableObject, IQueryAttributable
{
    [ObservableProperty]
    private Department thisDepartment = new Department { Id = "", Name = "" };

    [ObservableProperty]
    private string command = "";

    [ObservableProperty]
    private bool isInvisible = false;

    [ObservableProperty]
    private bool isReadOnly = false;

    [ObservableProperty]
    private string headOfDepartmentID = "";

    public AddDepartmentPageViewModel(IDepartmentRepository departmentRepository)
    {
        _departmentRepository = departmentRepository;
    }

    async void IQueryAttributable.ApplyQueryAttributes(IDictionary<string, object> query)
    {
        Command = (string)query["command"];
        if (Command == "edit")
        {
            string ThisDepartmentID = (string)query["departmentID"];
            ThisDepartment = await _departmentRepository.GetDepartmentByIdAsync(ThisDepartmentID) ?? new Department { Name = "" };
            HeadOfDepartmentID = ThisDepartment.HeadOfDepartmentId ?? "Not selected yet";
            IsInvisible = true;
            IsReadOnly = true;
        }

        if (query.ContainsKey("headOfDepartmentID"))
        {
            HeadOfDepartmentID = (string)query["headOfDepartmentID"];
        }
    }

    private readonly IDepartmentRepository _departmentRepository;

    [RelayCommand]
    async Task Submit()
    {
        if (string.IsNullOrEmpty(ThisDepartment.Name))
        {
            await Shell.Current.DisplayAlert("Error", "Please enter a department name", "OK");
            return;
        }
        if (string.IsNullOrEmpty(ThisDepartment.Id))
        {
            await Shell.Current.DisplayAlert("Error", "Please enter a department id", "OK");
            return;
        }

        if (Command != "edit")
        {
            bool check = await _departmentRepository.CheckValidID(ThisDepartment.Id);
            if (!check)
            {
                await Shell.Current.DisplayAlert("Error", "Department ID already exists", "OK");
                return;
            }
        }

        await Shell.Current.GoToAsync($"..", new Dictionary<string, object>
        {
            { Command, ThisDepartment }
        });
    }

    [RelayCommand]
    async Task Cancel()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    async Task HeadOfDepartmentTapped()
    {
        if (command == "add")
            await Shell.Current.DisplayAlert("Error", "Cannot add Head ID for new department", "OK");
        if (command == "edit")
            await Shell.Current.DisplayAlert("Error", "Cannot modify the Head ID", "OK");
    }

    [RelayCommand]
    async Task ChooseHead()
    {
        int numberOfEmployees = await _departmentRepository.GetTotalDepartmentEmployees(ThisDepartment.Id);
        if (numberOfEmployees == 0)
        {
            await Shell.Current.DisplayAlert("Error", "No employee in this department", "OK");
            return;
        }
        await Shell.Current.GoToAsync($"{nameof(SelectHeadOfDepartmentPage)}", new Dictionary<string, object>
        {
            { "departmentID", ThisDepartment.Id }
        });
    }
}
using PRM.ConsoleUI.Services;

using PRM.ConsoleUI.UI.Helpers;

using PRM.Models.DTOs.Auth;



namespace PRM.ConsoleUI.UI.Screens;



public class LoginScreen

{

    private readonly AuthApiClient _authApiClient;



    public LoginScreen(AuthApiClient authApiClient)

    {

        _authApiClient = authApiClient;

    }



    public async Task<bool> ShowAsync()

    {

        while (true)

        {

            ConsoleHelper.WriteHeader(

                "Project & Resource Management Tool",

                "Learn & Code - Final Project");



            Console.WriteLine("1. Login");

            Console.WriteLine("2. Exit");

            Console.WriteLine();

            Console.Write("Enter option: ");



            var choice = Console.ReadLine()?.Trim();



            switch (choice)

            {

                case "1":

                    if (await HandleLoginAsync())

                    {

                        return true;

                    }



                    break;

                case "2":

                    throw new ApplicationException("Exit");

                default:

                    ConsoleHelper.WriteError("Invalid option.");

                    ConsoleHelper.Pause();

                    break;

            }

        }

    }



    private async Task<bool> HandleLoginAsync()

    {

        ConsoleHelper.WriteHeader("Login");



        var username = ConsoleHelper.ReadInput("Username");

        var password = ConsoleHelper.ReadPassword("Password");



        try

        {

            await _authApiClient.LoginAsync(new LoginRequest

            {

                Username = username,

                Password = password

            });



            return true;

        }

        catch (Exception ex)

        {

            ConsoleHelper.WriteError(ex.Message);

            ConsoleHelper.Pause();

            return false;

        }

    }

}



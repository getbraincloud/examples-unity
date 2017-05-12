/**
 * Helpers to access debug input fields in this test application
 */

using System;

public abstract class UtilValues
{
    public static string getPassword()
    {
        return ErrorHandlingApp.getInstance().m_mainPage.m_loginValueSection.m_password;
    }


    public static string getUniversal_1()
    {
        return ErrorHandlingApp.getInstance().m_mainPage.m_loginValueSection.m_universal_1;
    }

    public static string getUniversal_2()
    {
        return ErrorHandlingApp.getInstance().m_mainPage.m_loginValueSection.m_universal_2;
    }

    public static string getEmail()
    {
        return ErrorHandlingApp.getInstance().m_mainPage.m_loginValueSection.m_email;
    }

    public static string getAccountLogin(ExampleAccountType exampleAccountType)
    {
        switch (exampleAccountType)
        {
            case ExampleAccountType.Universal_1:
            {
                return getUniversal_1();
            }
            case ExampleAccountType.Universal_2:
            {
                return getUniversal_2();
            }
            case ExampleAccountType.Email:
            {
                return getEmail();
            }
            default:
            {
                throw new Exception();
            }
        }
    }
}
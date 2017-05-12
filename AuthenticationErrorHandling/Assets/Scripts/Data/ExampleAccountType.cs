//The list of accounts set up for testing in this example application

public enum ExampleAccountType
{
    Anonymous,
    Universal_1,
    Universal_2,
    Email
}

public abstract class UtilExampleAccountType
{
    public static string getTypeName(ExampleAccountType exampleType)
    {
        switch (exampleType)
        {
            case ExampleAccountType.Anonymous:
            {
                return "Anonymous";
            }
            case ExampleAccountType.Universal_1:
            case ExampleAccountType.Universal_2:
            {
                return "Universal";
            }
            case ExampleAccountType.Email:
            {
                return "Email";
            }
        }

        return "";
    }
}
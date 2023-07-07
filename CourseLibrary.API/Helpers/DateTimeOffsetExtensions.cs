
namespace CourseLibrary.API.Helpers;
public static class DateTimeOffsetExtensions
{
    public static int GetCurrentAge(this DateTimeOffset dateTimeOffset, 
        DateTimeOffset? dateOfDeath)
    {
        var currentDate = DateTime.UtcNow;
        int age = currentDate.Year - dateTimeOffset.Year;

        if (currentDate < dateTimeOffset.AddYears(age))
        {
            age--;
        }

        if (dateOfDeath != null)
        {
            age = dateOfDeath.Value.Year - dateTimeOffset.Year;
        }

        return age;
    }
    //public static int GetCurrentAge(this DateTimeOffset dateTimeOffset)
    //{
    //    var currentDate = DateTime.UtcNow;
    //    int age = currentDate.Year - dateTimeOffset.Year;

    //    if (currentDate < dateTimeOffset.AddYears(age))
    //    {
    //        age--;
    //    }

    //    return age;
    //}
}


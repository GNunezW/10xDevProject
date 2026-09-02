namespace plan_zajec_uczelnia.Models;

/// <summary>Stopień i typ studiów — determinuje maksymalną liczbę semestrów na kierunku.</summary>
public enum StudyDegree
{
    /// <summary>I stopień licencjackie — maks. 6 semestrów.</summary>
    FirstDegreeLicentiate = 1,

    /// <summary>I stopień inżynierskie — maks. 7 semestrów.</summary>
    FirstDegreeEngineer = 2,

    /// <summary>II stopień magisterskie — maks. 4 semestry.</summary>
    SecondDegreeMaster = 3
}

public static class StudyDegreeRules
{
    public static int MaxSemesters(StudyDegree degree) => degree switch
    {
        StudyDegree.FirstDegreeLicentiate => 6,
        StudyDegree.FirstDegreeEngineer => 7,
        StudyDegree.SecondDegreeMaster => 4,
        _ => 7
    };

    public static string Label(StudyDegree degree) => degree switch
    {
        StudyDegree.FirstDegreeLicentiate => "I stopień — licencjackie (max 6 sem.)",
        StudyDegree.FirstDegreeEngineer => "I stopień — inżynierskie (max 7 sem.)",
        StudyDegree.SecondDegreeMaster => "II stopień — magisterskie (max 4 sem.)",
        _ => degree.ToString()
    };

    public static string ShortLabel(StudyDegree degree) => degree switch
    {
        StudyDegree.FirstDegreeLicentiate => "Licencjackie",
        StudyDegree.FirstDegreeEngineer => "Inżynierskie",
        StudyDegree.SecondDegreeMaster => "Magisterskie",
        _ => degree.ToString()
    };

    public static void ValidateSemester(StudyDegree degree, int semester)
    {
        var max = MaxSemesters(degree);
        if (semester < 1 || semester > max)
            throw new InvalidOperationException(
                $"Semestr musi być między 1 a {max} dla studiów: {Label(degree)}.");
    }
}

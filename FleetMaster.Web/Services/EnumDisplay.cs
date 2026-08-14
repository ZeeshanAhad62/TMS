using System.Text;

namespace FleetMaster.Web.Services;

public static class EnumDisplay
{
    // Turns "ContainerCarrier" into "Container Carrier", keeps acronyms like "RC"/"CNG"/"EV" intact.
    public static string ToDisplayName(this Enum value)
    {
        var name = value.ToString();
        var sb = new StringBuilder();

        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (i > 0 && char.IsUpper(c) &&
                (char.IsLower(name[i - 1]) || (i + 1 < name.Length && char.IsLower(name[i + 1]) && char.IsUpper(name[i - 1]))))
            {
                sb.Append(' ');
            }
            sb.Append(c);
        }

        return sb.ToString();
    }
}

using System.IO;

public class FirewallHelper
{
    // Network configuration file type (consistent with the COM interface definition)
    private const int NET_FW_PROFILE2_DOMAIN = 1;
    private const int NET_FW_PROFILE2_PRIVATE = 2;
    private const int NET_FW_PROFILE2_PUBLIC = 4;

    /// <summary>
    /// Check if the current application is included in the firewall's allowed rules.
    /// </summary>
    /// <param name="direction">Rule direction: 1 = inbound, 2 = outbound, 0 = any</param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public static bool IsApplicationAllowed(int direction = 0)
    {
        string appPath = Path.GetFullPath(Environment.ProcessPath);
        Type policyType = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");

        if (policyType == null)
            throw new NotSupportedException("Unable to access Windows Firewall API.");

        dynamic fwPolicy = Activator.CreateInstance(policyType);
        dynamic rules = fwPolicy.Rules;

        // Obtain the current active network configuration file (bit mask)
        int currentProfiles = fwPolicy.CurrentProfileTypes;

        foreach (dynamic rule in rules)
        {
            try
            {
                // Check whether the rules are enabled and applicable to the current network configuration file
                if (!rule.Enabled || (rule.Profiles & currentProfiles) == 0)
                    continue;

                // Check if the rules are applicable to the current application
                if (!string.Equals(rule.ApplicationName?.ToString(), appPath,
                    StringComparison.OrdinalIgnoreCase))
                    continue;

                // Check direction (optional)
                if (direction != 0 && rule.Direction != direction)
                    continue;

                // Check if the action is permitted
                if (rule.Action == 1) // 1 = Allow
                    return true;
            }
            catch
            {
                // Ignore the inaccessible rules (due to permission issues)
                continue;
            }
        }

        return false;
    }
}
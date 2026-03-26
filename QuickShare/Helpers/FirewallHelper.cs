using System.IO;
using System.Collections.Generic;

public class FirewallHelper
{
    // Network configuration file type (consistent with the COM interface definition)
    private const int NET_FW_PROFILE2_DOMAIN = 1;
    private const int NET_FW_PROFILE2_PRIVATE = 2;
    private const int NET_FW_PROFILE2_PUBLIC = 4;

    /// <summary>
    /// 检查当前应用程序在防火墙的所有活动配置文件中是否都被允许。
    /// </summary>
    /// <param name="direction">Rule direction: 1 = inbound, 2 = outbound, 0 = any</param>
    /// <returns>仅当所有活动的网络配置文件中都有允许规则时返回true</returns>
    /// <exception cref="NotSupportedException"></exception>
    public static bool IsApplicationAllowed(int direction = 0)
    {
        string appPath = Path.GetFullPath(Environment.ProcessPath!);
        Type? policyType = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
        if (policyType == null)
            throw new NotSupportedException("Unable to access Windows Firewall API.");

        dynamic? fwPolicy = Activator.CreateInstance(policyType);
        if (fwPolicy == null)
            throw new NotSupportedException("Unable to access Windows Firewall API.");
        dynamic rules = fwPolicy.Rules;

        // 获取当前活动的网络配置文件位掩码
        int currentProfiles = fwPolicy.CurrentProfileTypes;

        // 如果没有任何活动的配置文件，返回true（没有防火墙限制）
        if (currentProfiles == 0) return true;

        // 确定当前活动了哪些网络配置文件
        List<int> activeProfiles = new List<int>();
        foreach (int profile in new int[] { NET_FW_PROFILE2_DOMAIN, NET_FW_PROFILE2_PRIVATE, NET_FW_PROFILE2_PUBLIC })
        {
            if ((currentProfiles & profile) != 0)
            {
                activeProfiles.Add(profile);
            }
        }

        // 初始化一个字典，记录每个活动配置文件中是否有允许规则
        Dictionary<int, bool> profileAllowed = new Dictionary<int, bool>();
        foreach (int profile in activeProfiles)
        {
            profileAllowed[profile] = false;
        }

        // 如果没有活动配置文件，返回true
        if (activeProfiles.Count == 0) return true;

        // 遍历所有规则
        foreach (dynamic rule in rules)
        {
            try
            {
                // 检查规则是否启用
                if (!rule.Enabled)
                    continue;

                // 检查是否适用于当前应用程序
                if (!string.Equals(rule.ApplicationName?.ToString(), appPath,
                    StringComparison.OrdinalIgnoreCase))
                    continue;

                // 检查方向（可选）
                if (direction != 0 && rule.Direction != direction)
                    continue;

                // 检查是否允许操作
                if (rule.Action != 1) // 1 = Allow
                    continue;

                // 获取此规则适用的配置文件
                int ruleProfiles = rule.Profiles;

                // 对于每个活动的配置文件，检查此规则是否适用
                foreach (int activeProfile in activeProfiles)
                {
                    if ((ruleProfiles & activeProfile) != 0)
                    {
                        // 此规则适用于此活动配置文件
                        profileAllowed[activeProfile] = true;
                    }
                }
            }
            catch
            {
                // 忽略不可访问的规则（由于权限问题）
                continue;
            }
        }

        // 检查所有活动配置文件是否都有至少一条允许规则
        foreach (bool isAllowed in profileAllowed.Values)
        {
            if (!isAllowed)
                return false;  // 至少有一个活动配置文件没有允许规则
        }

        return true;  // 所有活动配置文件都有允许规则
    }

    /// <summary>
    /// 获取每个网络配置文件的防火墙状态详细信息
    /// </summary>
    /// <param name="direction">Rule direction: 1 = inbound, 2 = outbound, 0 = any</param>
    /// <returns>包含每个配置文件状态的字典</returns>
    public static Dictionary<string, bool> GetProfileStatusDetails(int direction = 0)
    {
        string appPath = Path.GetFullPath(Environment.ProcessPath!);
        Type? policyType = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
        Dictionary<string, bool> result = new Dictionary<string, bool>();

        if (policyType == null)
            return result;

        dynamic? fwPolicy = Activator.CreateInstance(policyType);
        if (fwPolicy == null)
            return result;

        dynamic rules = fwPolicy.Rules;
        int currentProfiles = fwPolicy.CurrentProfileTypes;

        // 初始化所有可能的配置文件
        var profileDefs = new Dictionary<int, string>
        {
            { NET_FW_PROFILE2_DOMAIN, "Domain" },
            { NET_FW_PROFILE2_PRIVATE, "Private" },
            { NET_FW_PROFILE2_PUBLIC, "Public" }
        };

        // 初始化结果字典
        foreach (var def in profileDefs)
        {
            result[def.Value] = false;
        }

        // 遍历规则
        foreach (dynamic rule in rules)
        {
            try
            {
                if (!rule.Enabled) continue;
                if (!string.Equals(rule.ApplicationName?.ToString(), appPath, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (direction != 0 && rule.Direction != direction)
                    continue;
                if (rule.Action != 1) // 1 = Allow
                    continue;

                int ruleProfiles = rule.Profiles;

                // 标记此规则适用的配置文件
                foreach (var def in profileDefs)
                {
                    if ((ruleProfiles & def.Key) != 0)
                    {
                        result[def.Value] = true;
                    }
                }
            }
            catch
            {
                continue;
            }
        }

        return result;
    }
}
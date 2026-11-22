using System.Collections.Generic;

namespace UpgradeTool
{
    internal class Edition
    {
        public int Index;
        public string Name;
        public string Key;
        public string EditionId;
        public string ProductName;
        public string Composition;
        public string Group;

        public Edition(int index, string name, string key, string editionId,
                       string productName, string composition, string group)
        {
            Index = index;
            Name = name;
            Key = key;
            EditionId = editionId;
            ProductName = productName;
            Composition = composition;
            Group = group;
        }
    }

    internal static class Data
    {
        public const string AppName = "无损跨版本升级Windows";
        public const string Version = "V1.0.0";

        public const string Website = "https://zhcool.xyz";
        public const string RepoUrl = "https://github.com/ZHCOOL520/Windows-Edition-Upgrade";
        public const string QqMail = "zhcool520@qq.com";

        public const string ArgsNormal =
            "/eula accept /telemetry disable /priority normal /resizerecoverypartition enable";
        public const string ArgsForce =
            "/eula accept /telemetry disable /priority normal /resizerecoverypartition enable /pkey ";

        public static readonly List<Edition> Editions = Build();

        private static List<Edition> Build()
        {
            List<Edition> list = new List<Edition>();
            string g1 = "常规版本";
            list.Add(new Edition(1, "Windows 家庭版", "YTMG3-N6DKC-DKB77-7M9GH-8HVX7", "Core", "Windows 10 Home", "Core", g1));
            list.Add(new Edition(2, "Windows 专业版", "VK7JG-NPHTM-C97JM-9MPGT-3V66T", "Professional", "Windows 10 Pro", "Enterprise", g1));
            list.Add(new Edition(3, "Windows 专业工作站版", "DXG7C-N36C4-C4HTG-X4T3X-2YV77", "ProfessionalWorkstation", "Windows 10 Pro for Workstations", "Enterprise", g1));
            list.Add(new Edition(4, "Windows 企业版", "XGVPP-NMH47-7TTHJ-W3FW7-8HV2C", "Enterprise", "Windows 10 Enterprise", "Enterprise", g1));
            list.Add(new Edition(5, "Windows 专业教育版", "8PTT6-RNW4C-6V7J2-C2D3X-MHBPB", "ProfessionalEducation", "Windows 10 Pro Education", "Enterprise", g1));
            list.Add(new Edition(6, "Windows 教育版", "YNMGQ-8RYV3-4PGQ3-C8XTP-7CFBY", "Education", "Windows 10 Education", "Enterprise", g1));
            list.Add(new Edition(7, "Windows 企业版 多会话版", "CPWHC-NT2C7-VYW78-DHDB2-PG3GK", "ServerRdsh", "Windows 10 Enterprise multi-session", "Enterprise", g1));
            list.Add(new Edition(8, "Windows IoT 企业版", "XQQYW-NFFMW-XJPBH-K8732-CKFFD", "IoTEnterprise", "Windows 10 IoT Enterprise", "Enterprise", g1));
            list.Add(new Edition(9, "Windows 家庭版 单语言版", "BT79Q-G7N6G-PGBYW-4YWX6-6F4BT", "CoreSingleLanguage", "Windows 10 Home Single Language", "Core", g1));
            list.Add(new Edition(10, "Windows 11 SE 云版", "KY7PN-VR6RX-83W6Y-6DDYQ-T6R4W", "CloudEdition", "Windows 10 SE", "Enterprise", g1));
            list.Add(new Edition(11, "Windows 家庭版 N", "4CPRK-NM3K3-X6XXQ-RXX86-WXCHW", "CoreN", "Windows 10 Home N", "CoreN", g1));
            list.Add(new Edition(12, "Windows 专业版 N", "2B87N-8KFHP-DKV6R-Y2C8J-PKCKT", "ProfessionalN", "Windows 10 Pro N", "EnterpriseN", g1));
            list.Add(new Edition(13, "Windows 专业工作站版 N", "WYPNQ-8C467-V2W6J-TX4WX-WT2RQ", "ProfessionalWorkstationN", "Windows 10 Pro N for Workstations", "EnterpriseN", g1));
            list.Add(new Edition(14, "Windows 专业教育版 N", "GJTYN-HDMQY-FRR76-HVGC7-QPF8P", "ProfessionalEducationN", "Windows 10 Pro Education N", "EnterpriseN", g1));
            list.Add(new Edition(15, "Windows 教育版 N", "84NGF-MHBT6-FXBX8-QWJK7-DRR8H", "EducationN", "Windows 10 Education N", "EnterpriseN", g1));
            list.Add(new Edition(16, "Windows 企业版 N", "3V6Q6-NQXCX-V8YXR-9QCYV-QPFCT", "EnterpriseN", "Windows 10 Enterprise N", "EnterpriseN", g1));
            list.Add(new Edition(17, "Windows 11 SE 云版 N", "K9VKN-3BGWV-Y624W-MCRMQ-BHDCD", "CloudEditionN", "Windows 10 SE N", "EnterpriseN", g1));

            string g2 = "LTSC 与 Server（需要对应的独立安装介质）";
            list.Add(new Edition(18, "Windows 10 企业版 LTSC 2021", "M7XTQ-FN8P6-TTKYV-9D4CC-J462D", "EnterpriseS", "Windows 10 Enterprise LTSC 2021", "EnterpriseS", g2));
            list.Add(new Edition(19, "Windows 10 IoT 企业版 LTSC 2021", "QPM6N-7J2WJ-P88HH-P3YRH-YY74H", "IoTEnterpriseS", "Windows 10 IoT Enterprise LTSC 2021", "EnterpriseS", g2));
            list.Add(new Edition(20, "Windows 10 企业版 N LTSC 2021", "2D7NQ-3MDXF-9WTDT-X9CCP-CKD8V", "EnterpriseSN", "Windows 10 Enterprise N LTSC 2021", "EnterpriseSN", g2));
            list.Add(new Edition(21, "Windows Server 2022 标准版", "VDYBN-27WPP-V4HQT-9VMD4-VMK7H", "ServerStandard", "Windows Server 2022 Standard", "ServerStandard", g2));
            list.Add(new Edition(22, "Windows Server 2022 数据中心版", "WX4NM-KYWYW-QJJR4-XV3QB-6VM33", "ServerDatacenter", "Windows Server 2022 Datacenter", "ServerDatacenter", g2));
            list.Add(new Edition(23, "Windows 11 企业版 LTSC 2024", "M7XTQ-FN8P6-TTKYV-9D4CC-J462D", "EnterpriseS", "Windows 11 Enterprise LTSC", "EnterpriseS", g2));
            list.Add(new Edition(24, "Windows 11 IoT 企业版 LTSC 2024", "CGK42-GYN6Y-VD22B-BX98W-J8JXD", "IoTEnterpriseS", "Windows 11 IoT Enterprise LTSC", "EnterpriseS", g2));
            list.Add(new Edition(25, "Windows 11 企业版 N LTSC 2024", "92NFX-8DJQP-P6BBQ-THF9C-7CG2H", "EnterpriseSN", "Windows 11 Enterprise N LTSC", "EnterpriseSN", g2));
            list.Add(new Edition(26, "Windows Server 2025 标准版", "TVRH6-WHNXV-R9WG3-9XRFY-MY832", "ServerStandard", "Windows Server 2025 Standard", "ServerStandard", g2));
            list.Add(new Edition(27, "Windows Server 2025 数据中心版", "D764K-2NDRG-47T6Q-P8T8W-YP6DF", "ServerDatacenter", "Windows Server 2025 Datacenter", "ServerDatacenter", g2));
            return list;
        }
    }

    internal class MediaSource
    {
        public string Path;
        public string Label;

        public MediaSource(string path, string label)
        {
            Path = path;
            Label = label;
        }
    }

    internal enum RunMode
    {
        KeyOnly,
        Normal,
        Pkey,
        Force
    }
}

namespace Service.Helpers.DTOs.ServerTemplates
{
    public class SaveServerTemplateDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class UseServerTemplateDto
    {
        public string Name { get; set; } = string.Empty;
    }

    public class ServerTemplateDto
    {
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int UsageCount { get; set; }
        public int RoleCount { get; set; }
        public int CategoryCount { get; set; }
        public int ChannelCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime SyncedAt { get; set; }
        public bool OutOfDate { get; set; }
    }

    public class ServerTemplateInfoDto : ServerTemplateDto
    {
        public string ServerName { get; set; } = string.Empty;
        public string CreatorName { get; set; } = string.Empty;
        public List<ServerTemplateCategoryDto> Categories { get; set; } = new();
        public List<ServerTemplateChannelDto> Loose { get; set; } = new();
        public List<string> Roles { get; set; } = new();
    }

    public class ServerTemplateRoleDto
    {
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public long Permissions { get; set; }
        public bool DisplaySeparately { get; set; }
    }

    public class ServerTemplateChannelDto
    {
        public string Name { get; set; } = string.Empty;
        public string Kind { get; set; } = "text";
        public bool IsPrivate { get; set; }
        public int UserLimit { get; set; }
        public int SlowModeSeconds { get; set; }
    }

    public class ServerTemplateCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public bool IsPrivate { get; set; }
        public List<ServerTemplateChannelDto> Channels { get; set; } = new();
    }

    public class ServerTemplateSnapshotDto
    {
        public List<ServerTemplateRoleDto> Roles { get; set; } = new();
        public List<ServerTemplateCategoryDto> Categories { get; set; } = new();
        public List<ServerTemplateChannelDto> Loose { get; set; } = new();
    }
}

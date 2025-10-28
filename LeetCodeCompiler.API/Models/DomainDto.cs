namespace LeetCodeCompiler.API.Models
{
    public class DomainDto
    {
        public int DomainId { get; set; }
        public string DomainName { get; set; } = string.Empty;
        public List<SubdomainDto> Subdomains { get; set; } = new();
    }

    public class SubdomainDto
    {
        public int SubdomainId { get; set; }
        public int DomainId { get; set; }
        public string SubdomainName { get; set; } = string.Empty;
        public DomainBasicDto? Domain { get; set; }
    }

    public class DomainBasicDto
    {
        public int DomainId { get; set; }
        public string DomainName { get; set; } = string.Empty;
    }

    public class CreateDomainRequest
    {
        public string DomainName { get; set; } = string.Empty;
    }

    public class CreateSubdomainRequest
    {
        public int DomainId { get; set; }
        public string SubdomainName { get; set; } = string.Empty;
    }
}

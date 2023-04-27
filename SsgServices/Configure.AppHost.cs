using Funq;
using ServiceStack;
using SsgServices.ServiceInterface;
using SsgServices.ServiceModel;

[assembly: HostingStartup(typeof(SsgServices.AppHost))]

namespace SsgServices;

public class AppHost : AppHostBase, IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices((context,services) => {
            // Configure ASP.NET Core IOC Dependencies
            MailData.Instance = new MailData
            {
                BaseUrl = context.HostingEnvironment.IsDevelopment()
                    ? "https://localhost:5002"
                    : "https://servicestack.net"
            };
            services.AddSingleton(MailData.Instance);
            services.AddSingleton(AppData.Instance);
        });

    public AppHost() : base("SSG Services", typeof(MyServices).Assembly) {}

    public override void Configure(Container container)
    {
        SetConfig(new HostConfig {
            AddRedirectParamsToQueryString = true,
            UseSameSiteCookies = false,
            AllowFileExtensions = { "json" }
        });
        
        Plugins.Add(new CorsFeature(allowedHeaders: "Content-Type,Authorization",
            allowOriginWhitelist: new[]{
                "https://localhost:5002",
                "https://localhost:5001",
                "http://localhost:5000",
                "http://localhost:8080",
                "https://servicestack.net",
                "https://diffusion.works",
            }, allowCredentials: true));
        
        MarkdownConfig.Transformer = new MarkdigTransformer();
        LoadAsync(container).GetAwaiter().GetResult();
    }

    public async Task LoadAsync(Container container)
    {
        container.Resolve<MailData>().LoadAsync().GetAwaiter().GetResult();
        container.Resolve<AppData>().LoadAsync(ContentRootDirectory.GetDirectory("emails")).GetAwaiter().GetResult();
        ScriptContext.ScriptAssemblies.Add(typeof(Hello).Assembly);
        ScriptContext.Args[nameof(AppData)] = AppData.Instance;
    }
}

public class MarkdigTransformer : IMarkdownTransformer
{
    private Markdig.MarkdownPipeline Pipeline { get; } = 
        Markdig.MarkdownExtensions.UseAdvancedExtensions(new Markdig.MarkdownPipelineBuilder()).Build();
    public string Transform(string markdown) => Markdig.Markdown.ToHtml(markdown, Pipeline);
}
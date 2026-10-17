using BasicHud.Components;
using BasicHud.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Use the actual Razor sources and production _Imports, not a test copy of either component.
// RenderTree is the framework's renderer-test surface; no browser or third-party test runtime.
#pragma warning disable BL0006
static class RenderTests
{
    public static async Task Run(Action<bool, string> check)
    {
        using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        await using var renderer = new TestRenderer(services, services.GetRequiredService<ILoggerFactory>());
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var image = renderer.Attach(new ImageTile());
            await renderer.Render(image, new() { [nameof(ImageTile.Source)] = "/cast-media/first.png", [nameof(ImageTile.Label)] = "Alpha" });
            check(renderer.HasElement(image, "img"), "actual ImageTile Razor renders its supplied image");
            var imageHandler = renderer.ErrorHandler(image);
            check(imageHandler != 0, "actual product ImageTile registers a native onerror callback, never a literal @onerror attribute");
            await renderer.DispatchEventAsync(imageHandler, null, EventArgs.Empty);
            check(!renderer.HasElement(image, "img") && renderer.HasElement(image, "span") && renderer.HasText(image, "A"), "dispatching the registered image error renders the neutral monogram");
            await renderer.Render(image, new() { [nameof(ImageTile.Source)] = "/cast-media/first.png", [nameof(ImageTile.Label)] = "Alpha" });
            check(!renderer.HasElement(image, "img"), "rerendering the failed source retains the fallback instead of requesting it again");
            await renderer.Render(image, new() { [nameof(ImageTile.Source)] = "/cast-media/second.png", [nameof(ImageTile.Label)] = "Bravo" });
            check(renderer.HasElement(image, "img") && renderer.ErrorHandler(image) != 0, "a changed image source resets the failure and registers its callback");

            var radar = renderer.Attach(new Radar());
            string? failedOverview = null;
            const string overview = "/cast-media/overview.png";
            await renderer.Render(radar, new()
            {
                [nameof(Radar.View)] = new RadarView("Configured overview", overview, []),
                [nameof(Radar.OverviewFailed)] = EventCallback.Factory.Create<string>(new object(), value => failedOverview = value),
            });
            check(renderer.HasElement(radar, "image"), "actual Radar Razor renders its configured SVG overview");
            var overviewHandler = renderer.ErrorHandler(radar);
            check(overviewHandler != 0, "actual product Radar registers a native SVG image onerror callback");
            await renderer.DispatchEventAsync(overviewHandler, null, EventArgs.Empty);
            check(failedOverview == overview, "dispatching the SVG image error invokes OverviewFailed with the actual image identity");
        });
        if (renderer.Exception is { } exception) throw new InvalidOperationException("Actual component render or event failed.", exception);
    }

    sealed class TestRenderer(IServiceProvider services, ILoggerFactory logger) : Renderer(services, logger)
    {
        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();
        public Exception? Exception { get; private set; }
        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;
        protected override void HandleException(Exception exception) => Exception = exception;
        public int Attach(IComponent component) => AssignRootComponentId(component);
        public Task Render(int id, Dictionary<string, object?> parameters) => RenderRootComponentAsync(id, ParameterView.FromDictionary(parameters));
        public bool HasElement(int id, string name) => Frames(id).Any(frame => frame.FrameType == RenderTreeFrameType.Element && frame.ElementName == name);
        public bool HasText(int id, string text) => Frames(id).Any(frame => frame.FrameType == RenderTreeFrameType.Text && frame.TextContent == text);
        public ulong ErrorHandler(int id)
        {
            var attributes = Frames(id).Where(frame => frame.FrameType == RenderTreeFrameType.Attribute).ToArray();
            if (attributes.Any(frame => frame.AttributeName == "@onerror")) return 0;
            return attributes.SingleOrDefault(frame => frame.AttributeName == "onerror").AttributeEventHandlerId;
        }
        RenderTreeFrame[] Frames(int id)
        {
            var range = GetCurrentRenderTreeFrames(id);
            return range.Array.Take(range.Count).ToArray();
        }
    }
}
#pragma warning restore BL0006

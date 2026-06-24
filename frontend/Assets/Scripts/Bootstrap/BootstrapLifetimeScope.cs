using System;
using GameKit.DependencyInjection;
using GameKit.DependencyInjection.Base;
using DA.Activity;
using DA.Agent;
using DA.Conversation;
using DA.Logging;
using DA.ScreenCapture;
using DA.Speech;
using DA.Vision;
using DA.Watch;
using VContainer;
using VContainer.Unity;

namespace DA.Bootstrap
{
    public sealed class BootstrapLifetimeScope : BaseLifetimeScopeRegistration
    {
        public override Type GetParentType()
        {
            return typeof(RootLifetimeScope);
        }

        public override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<Bootstrapper>();

            builder.Register<IScreenCaptureService, WindowsScreenCaptureService>(Lifetime.Singleton);
            builder.Register<IImageDifferenceService, ImageDifferenceService>(Lifetime.Singleton);
            builder.Register<IVisionService, OllamaVisionService>(Lifetime.Singleton);
            builder.Register<IActivityEventClassifier, OllamaActivityEventClassifier>(Lifetime.Singleton);
            builder.Register<ActivityEpisodeTracker>(Lifetime.Singleton);
            builder.Register<IWatchCommentService, OllamaWatchCommentService>(Lifetime.Singleton);
            builder.Register<IWatchRecognitionService, OllamaWatchRecognitionService>(Lifetime.Singleton);
            builder.Register<IConversationService, OllamaConversationService>(Lifetime.Singleton);
            builder.Register<ISpeechSynthesisService, VoicevoxSpeechSynthesisService>(Lifetime.Singleton);
            builder.Register<AgentLog>(Lifetime.Singleton);
            builder.Register<AgentApplication>(Lifetime.Singleton);
        }
    }
}

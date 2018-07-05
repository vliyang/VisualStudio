﻿using System;
using System.Threading.Tasks;
using GitHub.Services;
using GitHub.VisualStudio.Commands;
using Microsoft.VisualStudio;
using NSubstitute;
using NUnit.Framework;

public class OpenFromClipboardCommandTests
{
    public class TheExecuteMethod
    {
        [Test]
        public async Task NothingInClipboard()
        {
            var vsServices = Substitute.For<IVSServices>();
            vsServices.ShowMessageBoxInfo(null).Returns(VSConstants.MessageBoxResult.IDOK);
            var target = CreateOpenFromClipboardCommand(vsServices: vsServices, contextFromClipboard: null);

            await target.Execute(null);

            vsServices.Received(1).ShowMessageBoxInfo(OpenFromClipboardCommand.NoGitHubUrlMessage);
        }

        [Test]
        public async Task NoLocalRepository()
        {
            var context = new GitHubContext();
            var repositoryDir = null as string;
            var vsServices = Substitute.For<IVSServices>();
            var target = CreateOpenFromClipboardCommand(vsServices: vsServices, contextFromClipboard: context, repositoryDir: repositoryDir);

            await target.Execute(null);

            vsServices.Received(1).ShowMessageBoxInfo(OpenFromClipboardCommand.NoActiveRepositoryMessage);
        }

        [Test]
        public async Task CouldNotResolve()
        {
            var context = new GitHubContext();
            var repositoryDir = "repositoryDir";
            (string, string)? gitObject = null;
            var vsServices = Substitute.For<IVSServices>();
            var target = CreateOpenFromClipboardCommand(vsServices: vsServices,
                contextFromClipboard: context, repositoryDir: repositoryDir, gitObject: gitObject);

            await target.Execute(null);

            vsServices.Received(1).ShowMessageBoxInfo(OpenFromClipboardCommand.NoResolveMessage);
        }

        [Test]
        public async Task CouldResolve()
        {
            var context = new GitHubContext();
            var repositoryDir = "repositoryDir";
            var gitObject = ("master", "foo.cs");
            var vsServices = Substitute.For<IVSServices>();
            var target = CreateOpenFromClipboardCommand(vsServices: vsServices,
                contextFromClipboard: context, repositoryDir: repositoryDir, gitObject: gitObject);

            await target.Execute(null);

            vsServices.DidNotReceiveWithAnyArgs().ShowMessageBoxInfo(null);
        }

        [Test]
        public async Task NoChangesInWorkingDirectory()
        {
            var gitHubContextService = Substitute.For<IGitHubContextService>();
            var context = new GitHubContext();
            var repositoryDir = "repositoryDir";
            var gitObject = ("master", "foo.cs");
            var vsServices = Substitute.For<IVSServices>();
            var target = CreateOpenFromClipboardCommand(gitHubContextService: gitHubContextService, vsServices: vsServices,
                contextFromClipboard: context, repositoryDir: repositoryDir, gitObject: gitObject, hasChanges: false);

            await target.Execute(null);

            vsServices.DidNotReceiveWithAnyArgs().ShowMessageBoxInfo(null);
            gitHubContextService.Received(1).TryOpenFile(repositoryDir, context);
        }

        [Test]
        public async Task HasChangesInWorkingDirectory()
        {
            var gitHubContextService = Substitute.For<IGitHubContextService>();
            var context = new GitHubContext();
            var repositoryDir = "repositoryDir";
            var gitObject = ("master", "foo.cs");
            var vsServices = Substitute.For<IVSServices>();
            var target = CreateOpenFromClipboardCommand(gitHubContextService: gitHubContextService, vsServices: vsServices,
                contextFromClipboard: context, repositoryDir: repositoryDir, gitObject: gitObject, hasChanges: true);

            await target.Execute(null);

            vsServices.Received(1).ShowMessageBoxInfo(OpenFromClipboardCommand.ChangesInWorkingDirectoryMessage);
            gitHubContextService.Received(1).TryOpenFile(repositoryDir, context);
        }

        static OpenFromClipboardCommand CreateOpenFromClipboardCommand(
            IGitHubContextService gitHubContextService = null,
            ITeamExplorerContext teamExplorerContext = null,
            IVSServices vsServices = null,
            GitHubContext contextFromClipboard = null,
            string repositoryDir = null,
            (string, string)? gitObject = null,
            bool? hasChanges = null)
        {
            var sp = Substitute.For<IServiceProvider>();
            gitHubContextService = gitHubContextService ?? Substitute.For<IGitHubContextService>();
            teamExplorerContext = teamExplorerContext ?? Substitute.For<ITeamExplorerContext>();
            vsServices = vsServices ?? Substitute.For<IVSServices>();

            gitHubContextService.FindContextFromClipboard().Returns(contextFromClipboard);
            teamExplorerContext.ActiveRepository.LocalPath.Returns(repositoryDir);
            if (gitObject != null)
            {
                gitHubContextService.ResolveGitObject(repositoryDir, contextFromClipboard).Returns(gitObject.Value);
            }

            if (hasChanges != null)
            {
                gitHubContextService.HasChangesInWorkingDirectory(repositoryDir, gitObject.Value.Item1, gitObject.Value.Item2).Returns(hasChanges.Value);
            }

            return new OpenFromClipboardCommand(
                new Lazy<IGitHubContextService>(() => gitHubContextService),
                new Lazy<ITeamExplorerContext>(() => teamExplorerContext),
                new Lazy<IVSServices>(() => vsServices));
        }
    }
}

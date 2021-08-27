using System;
using FluentAssertions;
using maple_server_hotfix.Services;
using Xunit;

namespace maple_server_hotfix.Tests
{
    public class WindowsFileProviderTests
    {
        [Fact]
        public void PreventDirectoryTraversal()
        {
            // Arrange
            var provider = new WindowsFileProvider();

            // Act
            Action act = () => provider.Get("../../123", "abc");

            // Assert
            act.Should().Throw<Exception>().WithMessage("Invalid file name!");
        }
    }
}
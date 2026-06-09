using Bellerophon.Core.Player;
using NUnit.Framework;

namespace Bellerophon.Tests.EditMode
{
    public sealed class FirstPersonPlayerInputTests
    {
        [Test]
        public void GameplayActions_AreSuppressedWhenEscapeUnlocksCursor()
        {
            Assert.That(FirstPersonPlayerInput.IsGameplayActionSuppressedForValidation(
                false,
                false,
                false), Is.False);

            Assert.That(FirstPersonPlayerInput.IsGameplayActionSuppressedForValidation(
                false,
                false,
                true), Is.True);

            Assert.That(FirstPersonPlayerInput.IsGameplayActionSuppressedForValidation(
                true,
                false,
                false), Is.True);

            Assert.That(FirstPersonPlayerInput.IsGameplayActionSuppressedForValidation(
                false,
                true,
                false), Is.True);
        }
    }
}

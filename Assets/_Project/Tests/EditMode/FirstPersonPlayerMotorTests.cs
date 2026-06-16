using Bellerophon.Core.Player;
using NUnit.Framework;
using UnityEngine;

namespace Bellerophon.Tests.EditMode
{
    public sealed class FirstPersonPlayerMotorTests
    {
        [Test]
        public void PlaytestFreeMoveDirection_CombinesCameraRelativeAndVerticalMovement()
        {
            var direction = FirstPersonPlayerMotor.CalculatePlaytestFreeMoveDirectionForValidation(
                Vector3.forward,
                Vector3.right,
                new Vector2(1f, 1f),
                true,
                false);

            Assert.That(direction.magnitude, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(direction.x, Is.GreaterThan(0f));
            Assert.That(direction.y, Is.GreaterThan(0f));
            Assert.That(direction.z, Is.GreaterThan(0f));
        }

        [Test]
        public void PlaytestFreeMoveDirection_AllowsDescendingWithoutPlanarInput()
        {
            var direction = FirstPersonPlayerMotor.CalculatePlaytestFreeMoveDirectionForValidation(
                Vector3.forward,
                Vector3.right,
                Vector2.zero,
                false,
                true);

            Assert.That(direction, Is.EqualTo(Vector3.down));
        }
    }
}

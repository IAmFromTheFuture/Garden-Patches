using NUnit.Framework;
using Patches.Domain;

namespace Patches.Tests
{
    [TestFixture]
    public class ValidatorTests
    {
        [Test]
        public void SquareValidator_ValidatesSquares()
        {
            var validator = ShapeValidatorFactory.GetValidator(ShapeType.Square);

            // Valid square: 2x2 = 4 area
            var validBounds = new PatchBounds(0, 1, 0, 1);
            Assert.IsTrue(validator.Validate(validBounds, 4));

            // Invalid shape (not square): 2x1 = 2 area
            var invalidShapeBounds = new PatchBounds(0, 1, 0, 0);
            Assert.IsFalse(validator.Validate(invalidShapeBounds, 2));

            // Invalid area: 2x2 = 4 area, but required area is 9
            Assert.IsFalse(validator.Validate(validBounds, 9));
        }

        [Test]
        public void TallValidator_ValidatesTallRectangles()
        {
            var validator = ShapeValidatorFactory.GetValidator(ShapeType.Tall);

            // Valid tall: 1x2 = 2 area (height > width)
            var validBounds = new PatchBounds(0, 0, 0, 1);
            Assert.IsTrue(validator.Validate(validBounds, 2));

            // Invalid tall (square): 2x2 = 4 area
            var squareBounds = new PatchBounds(0, 1, 0, 1);
            Assert.IsFalse(validator.Validate(squareBounds, 4));

            // Invalid tall (wide): 2x1 = 2 area (width > height)
            var wideBounds = new PatchBounds(0, 1, 0, 0);
            Assert.IsFalse(validator.Validate(wideBounds, 2));
        }

        [Test]
        public void WideValidator_ValidatesWideRectangles()
        {
            var validator = ShapeValidatorFactory.GetValidator(ShapeType.Wide);

            // Valid wide: 2x1 = 2 area (width > height)
            var validBounds = new PatchBounds(0, 1, 0, 0);
            Assert.IsTrue(validator.Validate(validBounds, 2));

            // Invalid wide (square): 2x2 = 4 area
            var squareBounds = new PatchBounds(0, 1, 0, 1);
            Assert.IsFalse(validator.Validate(squareBounds, 4));

            // Invalid wide (tall): 1x2 = 2 area (height > width)
            var tallBounds = new PatchBounds(0, 0, 0, 1);
            Assert.IsFalse(validator.Validate(tallBounds, 2));
        }

        [Test]
        public void FreeformValidator_ValidatesAnyRectangleOfRequiredArea()
        {
            var validator = ShapeValidatorFactory.GetValidator(ShapeType.Freeform);

            // Valid square: 2x2 = 4 area
            var squareBounds = new PatchBounds(0, 1, 0, 1);
            Assert.IsTrue(validator.Validate(squareBounds, 4));

            // Valid tall: 1x4 = 4 area
            var tallBounds = new PatchBounds(0, 0, 0, 3);
            Assert.IsTrue(validator.Validate(tallBounds, 4));

            // Valid wide: 4x1 = 4 area
            var wideBounds = new PatchBounds(0, 3, 0, 0);
            Assert.IsTrue(validator.Validate(wideBounds, 4));

            // Invalid area: 2x2 = 4 area, required is 5
            Assert.IsFalse(validator.Validate(squareBounds, 5));
        }
    }
}

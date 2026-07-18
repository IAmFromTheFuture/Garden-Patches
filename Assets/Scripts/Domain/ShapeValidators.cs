using System;

namespace Patches.Domain
{
    public interface IShapeValidator
    {
        bool Validate(PatchBounds bounds, int requiredArea);
    }

    public class SquareValidator : IShapeValidator
    {
        public bool Validate(PatchBounds bounds, int requiredArea) =>
            bounds.Width == bounds.Height && bounds.Area == requiredArea;
    }

    public class TallValidator : IShapeValidator
    {
        public bool Validate(PatchBounds bounds, int requiredArea) =>
            bounds.Height > bounds.Width && bounds.Area == requiredArea;
    }

    public class WideValidator : IShapeValidator
    {
        public bool Validate(PatchBounds bounds, int requiredArea) =>
            bounds.Width > bounds.Height && bounds.Area == requiredArea;
    }

    public class FreeformValidator : IShapeValidator
    {
        public bool Validate(PatchBounds bounds, int requiredArea) =>
            bounds.Area == requiredArea;
    }

    public static class ShapeValidatorFactory
    {
        private static readonly SquareValidator _squareValidator = new SquareValidator();
        private static readonly TallValidator _tallValidator = new TallValidator();
        private static readonly WideValidator _wideValidator = new WideValidator();
        private static readonly FreeformValidator _freeformValidator = new FreeformValidator();

        public static IShapeValidator GetValidator(ShapeType type)
        {
            switch (type)
            {
                case ShapeType.Square:
                    return _squareValidator;
                case ShapeType.Tall:
                    return _tallValidator;
                case ShapeType.Wide:
                    return _wideValidator;
                case ShapeType.Freeform:
                    return _freeformValidator;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}

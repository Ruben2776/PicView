namespace PicView.Core.ImageTransformations
{
    public static class RotationHelper
    {
        /// <summary>
        /// This method checks if the provided rotation angle is valid or not.
        /// A rotation angle is considered valid if it is a multiple of 90 degrees and lies within the range of 0 to 360 degrees (inclusive).
        /// If the provided angle is not valid, the method returns false. Otherwise, it returns true.
        /// </summary>
        /// <param name="rotationAngle">A double value representing the rotation angle to be checked.</param>
        /// <returns>A bool value representing whether the provided rotation angle is valid or not.</returns>
        public static bool IsValidRotation(double rotationAngle)
        {
            rotationAngle %= 360;
            return rotationAngle is 0 or 90 or 180 or 270;
        }

        /// <summary>
        /// This method returns the next rotation angle based on the current rotation angle and the direction of rotation.
        /// </summary>
        /// <param name="currentDegrees">The current rotation angle in degrees.</param>
        /// <param name="clockWise">A boolean value indicating the direction of rotation. If true, the rotation is clockwise; otherwise, it is counterclockwise.</param>
        /// <returns>The next rotation angle in degrees.</returns>
        public static int NextRotationAngle(double currentDegrees, bool clockWise)
        {
            if (clockWise)
            {
                return currentDegrees switch
                {
                    > 0 and < 90 => 90,
                    > 90 and < 180 => 180,
                    > 180 and < 270 => 270,
                    _ => 0
                };
            }

            return currentDegrees switch
            {
                > 0 and < 90 => 0,
                > 90 and < 180 => 90,
                > 180 and < 270 => 180,
                _ => 270
            };
        }
    }
}
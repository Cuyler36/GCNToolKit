namespace GCNToolKit.Formats
{
    public static class SwizzleUtil
    {
        public static T[] Unswizzle<T>(T[] Input, int Width, int Height, int PixelsPerBlockW = 8, int PixelsPerBlockH = 8)
        {
            if (Width * Height > Input.Length)
            {
                throw new System.Exception("There are not enough elements in T[] Input for the specified Width and Height!"
                    + $"\nExpected a length of {Width * Height}, but got a length of {Input.Length}!");
            }

            int BlockXCount = Width / PixelsPerBlockW;
            int BlockYCount = Height / PixelsPerBlockH;

            T[] OutputBuffer = new T[Input.Length];
            uint PixelIndex = 0;

            for (int YBlock = 0; YBlock < BlockYCount; YBlock++)
            {
                for (int XBlock = 0; XBlock < BlockXCount; XBlock++)
                {
                    for (int YPixel = 0; YPixel < PixelsPerBlockH; YPixel++)
                    {
                        for (int XPixel = 0; XPixel < PixelsPerBlockW; XPixel++)
                        {
                            int OutputBufferIndex = (Width * PixelsPerBlockH * YBlock) + YPixel * Width + XBlock * PixelsPerBlockW + XPixel;
                            OutputBuffer[OutputBufferIndex] = Input[PixelIndex];
                            PixelIndex++;
                        }
                    }
                }
            }

            return OutputBuffer;
        }

        public static T[] Swizzle<T>(T[] Input, int Width, int Height, int PixelsPerBlockW = 8, int PixelsPerBlockH = 8)
        {
            if (Width * Height > Input.Length)
            {
                throw new System.Exception(string.Format("There are not enough elements in T[] Input for the specified Width and Height!\n" +
                    "Width = {0} | Height = {1} | Width * Height = {2} | Input Array Length = {3}", Width, Height, Width * Height, Input.Length));
            }

            int BlockXCount = Width / PixelsPerBlockW;
            int BlockYCount = Height / PixelsPerBlockH;

            T[] OutputBuffer = new T[Input.Length];
            uint OutputBufferIndex = 0;

            for (int YBlock = 0; YBlock < BlockYCount; YBlock++)
            {
                for (int XBlock = 0; XBlock < BlockXCount; XBlock++)
                {
                    for (int YPixel = 0; YPixel < PixelsPerBlockH; YPixel++)
                    {
                        for (int XPixel = 0; XPixel < PixelsPerBlockW; XPixel++)
                        {
                            int PixelIndex = (Width * PixelsPerBlockH * YBlock) + YPixel * Width + XBlock * PixelsPerBlockW + XPixel;
                            OutputBuffer[OutputBufferIndex] = Input[PixelIndex];
                            OutputBufferIndex++;
                        }
                    }
                }
            }

            return OutputBuffer;
        }
    }
}

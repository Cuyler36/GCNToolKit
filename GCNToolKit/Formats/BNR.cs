using System;
using System.Text;
using System.Drawing;
using System.Windows.Media.Imaging;
using GCNToolKit.Formats.Colors;
using GCNToolKit.Utilities;

namespace GCNToolKit.Formats
{
    /*
     * BNR File Structure 
     *  char[0x4] Magic = 'BNR1' OR 'BNR2' (BNR2 contains multiple descriptions in various languages which are 0x140 bytes long per)
     *  byte[0x1C] Padding = Always 0?
     *  ushort[0xC00] Image Data = RGB5A3 Image Data for the Game Banner Image (In Block Format)
     *  char[0x20] Game Name = The Short Name for the Game
     *  char[0x20] Company = The Company or Developer Name
     *  char[0x40] Full Game Title = The Full Game Name
     *  char[0x40] Company Full = The Full Company/Developer Name or description
     *  char[0x80] Game Description = A description of the game
     */

    public class BNR
    {
        private int fileSize = 0x1960;
        private string magic = "BNR1";
        private Bitmap bannerBitmap;
        private BitmapSource bannerImage;

        public char[] Magic;
        public byte[] Padding;
        public ushort[] ImageData;
        public char[] GameName;
        public char[] Company;
        public char[] Title;
        public char[] CompanyFull;
        public char[] Description;

        public BNR()
        {
            Magic = magic.ToCharArray();
            Padding = new byte[0x1C];
            ImageData = new ushort[0xC00];
            GameName = new char[0x20];
            Company = new char[0x20];
            Title = new char[0x40];
            CompanyFull = new char[0x40];
            Description = new char[0x80];
        }

        public BNR(byte[] Data)
        {
            if (Data.Length == fileSize)
            {
                Magic = new char[4] { (char)Data[0], (char)Data[1], (char)Data[2], (char)Data[3] };
                if (new string(Magic).Equals(magic))
                {
                    Padding = new byte[0x1C];
                    for (int i = 0; i < 0x1C; i++)
                        Padding[i] = Data[4 + i];

                    ImageData = new ushort[0xC00];
                    // Convert Image Data to ushorts
                    for (int i = 0; i < 0xC00; i++)
                    {
                        int idx = i * 2;
                        ImageData[i] = (ushort)((Data[0x20 + idx] << 8) | Data[0x20 + idx + 1]);
                    }

                    GameName = new char[0x20];
                    for (int i = 0; i < 0x20; i++)
                        GameName[i] = (char)Data[0x1820 + i];

                    Company = new char[0x20];
                    for (int i = 0; i < 0x20; i++)
                        Company[i] = (char)Data[0x1840 + i];

                    Title = new char[0x40];
                    for (int i = 0; i < 0x40; i++)
                        Title[i] = (char)Data[0x1860 + i];

                    CompanyFull = new char[0x40];
                    for (int i = 0; i < 0x40; i++)
                        CompanyFull[i] = (char)Data[0x18A0 + i];

                    Description = new char[0x80];
                    for (int i = 0; i < 0x80; i++)
                        Description[i] = (char)Data[0x18E0 + i];
                }
                else
                {
                    System.Windows.MessageBox.Show("The file does not appear to be a valid banner file! It cannot be opened.", "Banner File Error",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private static ushort[] DecodeBNRImage(ushort[] Buffer)
        {
            ushort[] BNR_Buffer = new ushort[Buffer.Length];
            int idx = 0;

            for (int yBlock = 0; yBlock < 32; yBlock += 4)
            {
                for (int xBlock = 0; xBlock < 96; xBlock += 4)
                {
                    for (int y = 0; y < 4; y++, idx += 4)
                    {
                        for (int x = 0; x < 4; x++)
                        {
                            BNR_Buffer[(yBlock + y) * 96 + (xBlock + x)] = Buffer[idx + x];
                        }
                    }
                }
            }

            return BNR_Buffer;
        }

        private static ushort[] EncodeBNRImage(ushort[] Buffer)
        {
            ushort[] BNR_Buffer = new ushort[Buffer.Length];
            int idx = 0;

            for (int yBlock = 0; yBlock < 32; yBlock += 4)
            {
                for (int xBlock = 0; xBlock < 96; xBlock += 4)
                {
                    for (int y = 0; y < 4; y++, idx += 4)
                    {
                        for (int x = 0; x < 4; x++)
                        {
                            BNR_Buffer[idx + x] = Buffer[(yBlock + y) * 96 + (xBlock + x)];
                        }
                    }
                }
            }

            return BNR_Buffer;
        }

        private void CreateBannerImage()
        {
            if (bannerBitmap != null)
                bannerBitmap.Dispose();

            ushort[] DecodedImageData = DecodeBNRImage(ImageData); //C4.DecodeC4(ImageData, 96, 32);
            uint[] PixelData = new uint[96 * 32];

            for (int i = 0; i < DecodedImageData.Length; i++)
            {
                PixelData[i] = RGB5A3.ToARGB8(DecodedImageData[i]);
            }

            byte[] BitmapSourceData = new byte[4 * 96 * 32];
            for (int i = 0; i < PixelData.Length; i++)
            {
                int idx = i * 4;
                BitmapSourceData[idx + 3] = (byte)((PixelData[i] >> 24) & 0xFF);
                BitmapSourceData[idx + 2] = (byte)((PixelData[i] >> 16) & 0xFF);
                BitmapSourceData[idx + 1] = (byte)((PixelData[i] >> 8) & 0xFF);
                BitmapSourceData[idx + 0] = (byte)((PixelData[i] >> 0) & 0xFF);
            }

            bannerBitmap = BitmapUtilities.CreateBitmap(BitmapSourceData, 96, 32);
            bannerImage = bannerBitmap.ToBitmapSource();
        }

        public void SetBannerImageData(ushort[] RGB5A3Pixels)
        {
            ImageData = EncodeBNRImage(RGB5A3Pixels);
        }

        public BitmapSource GetBannerImage()
        {
            if (bannerImage == null)
            {
                CreateBannerImage();
            }

            return bannerImage;
        }

        public Bitmap GetBannerImageBitmap()
        {
            if (bannerImage == null)
            {
                CreateBannerImage();
            }

            return bannerBitmap;
        }

        public BitmapSource RefreshBannerImage()
        {
            CreateBannerImage();
            return bannerImage;
        }

        public byte[] GetFileData()
        {
            byte[] Data = new byte[fileSize];

            Encoding.ASCII.GetBytes(magic).CopyTo(Data, 0);

            Padding.CopyTo(Data, 0x4);

            for (int i = 0; i < ImageData.Length; i++)
            {
                int idx = i * 2;
                Data[0x20 + idx] = (byte)((ImageData[i] >> 8) & 0xFF);
                Data[0x20 + idx + 1] = (byte)(ImageData[i] & 0xFF);
            }

            var GameNameData = Encoding.ASCII.GetBytes(GameName);
            Array.Resize(ref GameNameData, 0x20);
            GameNameData.CopyTo(Data, 0x1820);

            var CompanyData = Encoding.ASCII.GetBytes(Company);
            Array.Resize(ref CompanyData, 0x20);
            CompanyData.CopyTo(Data, 0x1840);

            var FullGameNameData = Encoding.ASCII.GetBytes(Title);
            Array.Resize(ref FullGameNameData, 0x40);
            FullGameNameData.CopyTo(Data, 0x1860);

            var FullCompanyData = Encoding.ASCII.GetBytes(CompanyFull);
            Array.Resize(ref FullCompanyData, 0x40);
            FullCompanyData.CopyTo(Data, 0x18A0);

            var DescriptionData = Encoding.ASCII.GetBytes(Description);
            Array.Resize(ref DescriptionData, 0x80);
            DescriptionData.CopyTo(Data, 0x18E0);

            return Data;
        }
    }
}

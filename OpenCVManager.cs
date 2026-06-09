using Euresys.Open_eVision;
using OpenCvSharp;
using OpenCvSharp.Features2D;
using ScottPlot;
using ScottPlot.Colormaps;
using ScottPlot.PlotStyles;
using ScottPlot.Plottables;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static SkiaSharp.HarfBuzz.SKShaper;


namespace WIA_ViewerProgram
{
    internal class OpenCVManager
    {
        string FilePath;
        Mat img;
        static OpenCvSharp.Point lastClickedPoint = new OpenCvSharp.Point();        
        static List<OpenCvSharp.Point> ReultsImgPoint = new List<OpenCvSharp.Point> {};
        private int ResultWidth = 1200;
        private int ResultHeight = 200;
        const int DC_row = 4;
        const int DC_col = 17;

        const int AC_row = 3;
        const int AC_col = 13;

        public List<int>[,] GridPointXY = { };
        public struct GearGridPoint
        {
            public int X;
            public int Y;
            public GearGridPoint(int x, int y) { X = x; Y = y; }
        }
        GearGridPoint[,] DC_grid;
        GearGridPoint[,] AC_grid;

        public OpenCVManager()
        {
            //이 grid 정보가 이후에는 레시피 별로 있어야함
            DC_grid = new GearGridPoint[DC_row, DC_col]
            {
                // [0행] 17개 데이터
                {
                    new(420, 1050),  new(520, 1059),  new(622, 1081),  new(723, 1106),
                    new(821, 1129),  new(916, 1159),  new(1003, 1190), new(1094, 1227),
                    new(1182, 1264), new(1266, 1301), new(1350, 1341), new(1435, 1384),
                    new(1508, 1425), new(1577, 1465), new(1650, 1508), new(1719, 1557),
                    new(1775, 1600)
                },
                // [1행] 17개 데이터
                {
                    new(408, 1135),  new(510, 1149),  new(608, 1168),  new(709, 1189),
                    new(802, 1212),  new(894, 1246),  new(978, 1277),  new(1064, 1313),
                    new(1153, 1348), new(1237, 1383), new(1320, 1422), new(1399, 1467),
                    new(1472, 1507), new(1542, 1548), new(1611, 1591), new(1683, 1641),
                    new(1753, 1683)
                },
                // [2행] 17개 데이터
                {
                    new(400, 1212),  new(502, 1226),  new(597, 1247),  new(695, 1268),
                    new(786, 1293),  new(875, 1324),  new(954, 1357),  new(1038, 1391),
                    new(1127, 1425), new(1212, 1461), new(1293, 1502), new(1367, 1544),
                    new(1439, 1588), new(1508, 1629), new(1576, 1669), new(1649, 1722),
                    new(1726, 1768)
                },
                // [3행] 17개 데이터
                {
                    new(389, 1296),  new(492, 1313),  new(585, 1328),  new(680, 1349),
                    new(766, 1374),  new(855, 1404),  new(931, 1433),  new(1014, 1464),
                    new(1103, 1499), new(1186, 1540), new(1265, 1579), new(1337, 1619),
                    new(1407, 1660), new(1477, 1702), new(1544, 1743), new(1616, 1795),
                    new(1698, 1853)
                }
            };


            AC_grid = new GearGridPoint[AC_row, AC_col]
            {
                {
                     new(421, 1116), new(557, 1127), new(697, 1126), new(825, 1119),
                     new(953, 1108), new(1077, 1085), new(1195, 1060), new(1316, 1032),
                     new(1441, 995), new(1552, 959), new(1668, 918), new(1785, 876),
                     new(1901, 834)
                 },
                 // [1행] 13개 데이터
                 {
                     new(468, 1216), new(601, 1221), new(739, 1218), new(869, 1213),
                     new(995, 1201), new(1120, 1181), new(1243, 1162), new(1364, 1135),
                     new(1489, 1101), new(1602, 1066), new(1719, 1027), new(1836, 985),
                     new(1953, 945)
                 },
                 // [2행] 13개 데이터
                 {
                     new(517, 1321), new(649, 1324), new(788, 1320), new(913, 1311),
                     new(1041, 1298), new(1169, 1283), new(1290, 1264), new(1412, 1236),
                     new(1535, 1201), new(1648, 1166), new(1764, 1125), new(1881, 1084),
                     new(1998, 1043)
                 }

            };



        }

        public Bitmap DC_GearGridWarpPerspective(string imgpath)
        {
            if (!File.Exists(imgpath))
            {
                MessageBox.Show(
                $"이미지 파일을 확인해주세요 \n {imgpath}",
                "이미지 파일 오류",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
                return null;
            }


            OpenCvSharp.Point Bojung = new OpenCvSharp.Point();


            img = Cv2.ImRead(imgpath);//이미지 초기화
            
            Mat tempdst = new Mat();
            Point2f[] srcPoints = new Point2f[4];
            Point2f[] dstPoints = new Point2f[4] {
            new Point2f(0, 0),
            new Point2f(ResultWidth/(DC_col-1), 0),
            new Point2f(ResultWidth/(DC_col-1), ResultHeight/(DC_row-1)),
            new Point2f(0, ResultHeight/(DC_row-1))
            };

            //이후에 삭제
            Mat Temp = Cv2.ImRead(imgpath);
            for (int i = 1; i < DC_col; i++)
            {
                Cv2.Line(Temp, new OpenCvSharp.Point(DC_grid[0, i - 1].X, DC_grid[0, i - 1].Y), new OpenCvSharp.Point(DC_grid[0, i].X, DC_grid[0, i].Y), new Scalar(0, 0, 255), 2, LineTypes.AntiAlias, 0);
            }

            for (int j = 1; j < DC_row; j++)
            {
                    Cv2.Line(Temp, new OpenCvSharp.Point(DC_grid[j - 1, 0].X, DC_grid[j - 1, 0].Y), new OpenCvSharp.Point(DC_grid[j, 0].X, DC_grid[j, 0].Y), new Scalar(0, 0, 255), 2, LineTypes.AntiAlias, 0);
            }



            for (int j = 1; j < DC_row; j++)
            {
                for (int i = 1; i < DC_col; i++)
                {

                    Cv2.Line(Temp, new OpenCvSharp.Point(DC_grid[j, i-1].X, DC_grid[j, i-1].Y), new OpenCvSharp.Point(DC_grid[j, i].X, DC_grid[j, i].Y), new Scalar(0, 0, 255), 2, LineTypes.AntiAlias, 0);
                    Cv2.Line(Temp, new OpenCvSharp.Point(DC_grid[j-1, i].X, DC_grid[j-1,i].Y), new OpenCvSharp.Point(DC_grid[j, i].X, DC_grid[j, i].Y), new Scalar(0, 0, 255), 2, LineTypes.AntiAlias, 0);
                } 
            }



            Mat hConnet = new Mat();
            Mat dst = new Mat();
            for (int j = 0; j < DC_row - 1; j++)
            {
                for (int i = 0; i < DC_col - 1; i++)
                {
                    srcPoints[0].X = DC_grid[j,i].X;
                    srcPoints[0].Y = DC_grid[j, i].Y;
                    srcPoints[1].X = DC_grid[j, i+1].X;
                    srcPoints[1].Y = DC_grid[j, i+1].Y;
                    srcPoints[2].X = DC_grid[j+1, i + 1].X;
                    srcPoints[2].Y = DC_grid[j+1, i + 1].Y;
                    srcPoints[3].X = DC_grid[j + 1, i].X;
                    srcPoints[3].Y = DC_grid[j + 1, i].Y;
                    // 4. 원근 변환 행렬 계산
                    Mat matrix = Cv2.GetPerspectiveTransform(srcPoints, dstPoints);
                    Cv2.WarpPerspective(img, tempdst, matrix, new OpenCvSharp.Size(ResultWidth / (DC_col - 1), ResultHeight / (DC_row - 1)));
                    if (i == 0)
                    {
                        hConnet = tempdst.Clone();
                    }
                    else
                    {
                        Cv2.HConcat(new Mat[] { hConnet, tempdst }, hConnet);
                    }

                }
                if (j == 0)
                {
                    dst = hConnet.Clone();
                }
                else
                {
                    Cv2.VConcat(new Mat[] { dst, hConnet }, dst);
                }
                hConnet.Dispose();
                hConnet = new Mat();
            }
            Cv2.ImShow("DC_Stitch", dst);
            Cv2.Resize(Temp, Temp,new OpenCvSharp.Size (1000,800), 0, 0, InterpolationFlags.Linear);
            Cv2.ImShow("DC_StitchArea", Temp);

            return OpenCvSharp.Extensions.BitmapConverter.ToBitmap(dst);
        }

        public Bitmap AC_GearGridWarpPerspective(string imgpath)
        {
            if (!File.Exists(imgpath))
            {
                MessageBox.Show(
                $"이미지 파일을 확인해주세요 \n {imgpath}",
                "이미지 파일 오류",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
                return null;
            }


            OpenCvSharp.Point Bojung = new OpenCvSharp.Point();


            img = Cv2.ImRead(imgpath);//이미지 초기화

            Mat tempdst = new Mat();
            Point2f[] srcPoints = new Point2f[4];
            Point2f[] dstPoints = new Point2f[4] {
            new Point2f(0, 0),
            new Point2f(ResultWidth/(AC_col-1), 0),
            new Point2f(ResultWidth/(AC_col-1), ResultHeight/(AC_row-1)),
            new Point2f(0, ResultHeight/(AC_row-1))
            };

            //이후에 삭제
            Mat Temp = Cv2.ImRead(imgpath);
            for (int i = 1; i < AC_col; i++)
            {
                Cv2.Line(Temp, new OpenCvSharp.Point(AC_grid[0, i - 1].X, AC_grid[0, i - 1].Y), new OpenCvSharp.Point(AC_grid[0, i].X, AC_grid[0, i].Y), new Scalar(0, 0, 255), 2, LineTypes.AntiAlias, 0);
            }

            for (int j = 1; j < AC_row; j++)
            {
                Cv2.Line(Temp, new OpenCvSharp.Point(AC_grid[j - 1, 0].X, AC_grid[j - 1, 0].Y), new OpenCvSharp.Point(AC_grid[j, 0].X, AC_grid[j, 0].Y), new Scalar(0, 0, 255), 2, LineTypes.AntiAlias, 0);
            }



            for (int j = 1; j < AC_row; j++)
            {
                for (int i = 1; i < AC_col; i++)
                {

                    Cv2.Line(Temp, new OpenCvSharp.Point(AC_grid[j, i - 1].X, AC_grid[j, i - 1].Y), new OpenCvSharp.Point(AC_grid[j, i].X, AC_grid[j, i].Y), new Scalar(0, 0, 255), 2, LineTypes.AntiAlias, 0);
                    Cv2.Line(Temp, new OpenCvSharp.Point(AC_grid[j - 1, i].X, AC_grid[j - 1, i].Y), new OpenCvSharp.Point(AC_grid[j, i].X, AC_grid[j, i].Y), new Scalar(0, 0, 255), 2, LineTypes.AntiAlias, 0);
                }
            }



            Mat hConnet = new Mat();
            Mat dst = new Mat();
            for (int j = 0; j < AC_row - 1; j++)
            {
                for (int i = 0; i < AC_col - 1; i++)
                {
                    srcPoints[0].X = AC_grid[j, i].X;
                    srcPoints[0].Y = AC_grid[j, i].Y;
                    srcPoints[1].X = AC_grid[j, i + 1].X;
                    srcPoints[1].Y = AC_grid[j, i + 1].Y;
                    srcPoints[2].X = AC_grid[j + 1, i + 1].X;
                    srcPoints[2].Y = AC_grid[j + 1, i + 1].Y;
                    srcPoints[3].X = AC_grid[j + 1, i].X;
                    srcPoints[3].Y = AC_grid[j + 1, i].Y;
                    // 4. 원근 변환 행렬 계산
                    Mat matrix = Cv2.GetPerspectiveTransform(srcPoints, dstPoints);
                    Cv2.WarpPerspective(img, tempdst, matrix, new OpenCvSharp.Size(ResultWidth / (AC_col - 1), ResultHeight / (AC_row - 1)));
                    if (i == 0)
                    {
                        hConnet = tempdst.Clone();
                    }
                    else
                    {
                        Cv2.HConcat(new Mat[] { hConnet, tempdst }, hConnet);
                    }

                }
                if (j == 0)
                {
                    dst = hConnet.Clone();
                }
                else
                {
                    Cv2.VConcat(new Mat[] { dst, hConnet }, dst);
                }
                hConnet.Dispose();
                hConnet = new Mat();
            }
            Cv2.ImShow("AC_Stitch", dst);
            Cv2.Resize(Temp, Temp, new OpenCvSharp.Size(1000, 800), 0, 0, InterpolationFlags.Linear);
            Cv2.ImShow("AC_StitchArea", Temp);

            return OpenCvSharp.Extensions.BitmapConverter.ToBitmap(dst);
        }
    }
}

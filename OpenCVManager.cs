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
        const int row = 4;
        const int col = 22;

        public List<int>[,] GridPointXY = { };
        public struct GearGridPoint
        {
            public int X;
            public int Y;
            public GearGridPoint(int x, int y) { X = x; Y = y; }
        }
        GearGridPoint[,] grid;

        public OpenCVManager()
        {
            //이 grid 정보가 이후에는 레시피 별로 있어야함
            grid = new GearGridPoint[row, col]
            {
                 // [0행] 22개 데이터
                 {
                     new(340, 918),   new(407, 938),   new(481, 961),   new(555, 985),
                     new(627, 1011),  new(701, 1036),  new(771, 1060),  new(844, 1087),
                     new(934, 1120),  new(1005, 1147), new(1075, 1176), new(1146, 1207),
                     new(1216, 1239), new(1286, 1272), new(1356, 1304), new(1426, 1340),
                     new(1494, 1373), new(1563, 1408), new(1642, 1449), new(1711, 1485),
                     new(1779, 1524), new(1848, 1563)
                 },
                 // [1행] 22개 데이터
                 {
                     new(309, 985),   new(380, 1006),  new(454, 1028),  new(527, 1050),
                     new(600, 1074),  new(673, 1098),  new(746, 1123),  new(819, 1149),
                     new(902, 1182),  new(974, 1211),  new(1044, 1240), new(1115, 1271),
                     new(1185, 1302), new(1255, 1335), new(1324, 1366), new(1395, 1401),
                     new(1464, 1434), new(1534, 1468), new(1611, 1508), new(1678, 1546),
                     new(1747, 1582), new(1814, 1622)
                 },
                 // [2행] 22개 데이터
                 {
                     new(280, 1055),  new(352, 1073),  new(425, 1096),  new(500, 1117),
                     new(572, 1141),  new(645, 1164),  new(718, 1188),  new(790, 1214),
                     new(872, 1247),  new(943, 1275),  new(1014, 1305), new(1083, 1335),
                     new(1153, 1366), new(1222, 1398), new(1293, 1431), new(1362, 1463),
                     new(1431, 1498), new(1510, 1535), new(1577, 1571), new(1644, 1608),
                     new(1712, 1646), new(1780, 1684)
                 },
                 // [3행] 22개 데이터
                 {
                     new(251, 1121),  new(326, 1142),  new(397, 1163),  new(471, 1184),
                     new(544, 1206),  new(617, 1231),  new(690, 1255),  new(762, 1280),
                     new(843, 1311),  new(911, 1340),  new(982, 1369),  new(1052, 1399),
                     new(1122, 1430), new(1191, 1461), new(1262, 1494), new(1330, 1527),
                     new(1399, 1560), new(1476, 1599), new(1542, 1634), new(1610, 1671),
                     new(1677, 1709), new(1746, 1746)
                 }
             };



        }

        public Bitmap GearGridWarpPerspective(string imgpath)
        {
            OpenCvSharp.Point Bojung = new OpenCvSharp.Point();


            img = Cv2.ImRead(imgpath);//이미지 초기화
            
            Mat tempdst = new Mat();
            Point2f[] srcPoints = new Point2f[4];
            Point2f[] dstPoints = new Point2f[4] {
            new Point2f(0, 0),
            new Point2f(ResultWidth/(col-1), 0),
            new Point2f(ResultWidth/(col-1), ResultHeight/(row-1)),
            new Point2f(0, ResultHeight/(row-1))
            };

            //이후에 삭제
            Mat Temp = Cv2.ImRead(imgpath);
            for (int i = 1; i < col; i++)
            {
                Cv2.Line(Temp, new OpenCvSharp.Point(grid[0, i - 1].X, grid[0, i - 1].Y), new OpenCvSharp.Point(grid[0, i].X, grid[0, i].Y), new Scalar(0, 0, 255), 2, LineTypes.AntiAlias, 0);
            }

            for (int j = 1; j < row; j++)
            {
                    Cv2.Line(Temp, new OpenCvSharp.Point(grid[j - 1, 0].X, grid[j - 1, 0].Y), new OpenCvSharp.Point(grid[j, 0].X, grid[j, 0].Y), new Scalar(0, 0, 255), 2, LineTypes.AntiAlias, 0);
            }



            for (int j = 1; j < row; j++)
            {
                for (int i = 1; i < col; i++)
                {

                    Cv2.Line(Temp, new OpenCvSharp.Point(grid[j, i-1].X, grid[j, i-1].Y), new OpenCvSharp.Point(grid[j, i].X, grid[j, i].Y), new Scalar(0, 0, 255), 2, LineTypes.AntiAlias, 0);
                    Cv2.Line(Temp, new OpenCvSharp.Point(grid[j-1, i].X, grid[j-1,i].Y), new OpenCvSharp.Point(grid[j, i].X, grid[j, i].Y), new Scalar(0, 0, 255), 2, LineTypes.AntiAlias, 0);
                } 
            }



            Mat hConnet = new Mat();
            Mat dst = new Mat();
            for (int j = 0; j < row-1; j++)
            {
                for (int i = 0; i < col-1; i++)
                {
                    srcPoints[0].X = grid[j,i].X;
                    srcPoints[0].Y = grid[j, i].Y;
                    srcPoints[1].X = grid[j, i+1].X;
                    srcPoints[1].Y = grid[j, i+1].Y;
                    srcPoints[2].X = grid[j+1, i + 1].X;
                    srcPoints[2].Y = grid[j+1, i + 1].Y;
                    srcPoints[3].X = grid[j + 1, i].X;
                    srcPoints[3].Y = grid[j + 1, i].Y;
                    // 4. 원근 변환 행렬 계산
                    Mat matrix = Cv2.GetPerspectiveTransform(srcPoints, dstPoints);
                    Cv2.WarpPerspective(img, tempdst, matrix, new OpenCvSharp.Size(ResultWidth / (col-1), ResultHeight / (row-1)));
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
            //Cv2.ImShow("dst", dst);
            Cv2.Resize(Temp, Temp,new OpenCvSharp.Size (1000,800), 0, 0, InterpolationFlags.Linear);
            Cv2.ImShow("Temp", Temp);

            return OpenCvSharp.Extensions.BitmapConverter.ToBitmap(dst);
        }


    }
}

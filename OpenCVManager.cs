using OpenCvSharp;
using ScottPlot;
using ScottPlot.Colormaps;
using ScottPlot.PlotStyles;
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
        public  List<OpenCvSharp.Point> GridPoint = new List<OpenCvSharp.Point> { };
        static List<OpenCvSharp.Point> ReultsImgPoint = new List<OpenCvSharp.Point> {};
        private int ResultWidth = 1000;
        private int ResultHeight = 300;
        public int GridCount = 44;


        public OpenCVManager()
        {
            OpenCvSharp.Point Temp = new OpenCvSharp.Point();
            for (int j = 0; j < 4; j++)
            {
                for (int i = 0; i < 11; i++)
                 {                
                    Temp.X = ResultWidth / 10 * i;
                    Temp.Y = ResultHeight / 3 * j;
                    ReultsImgPoint.Add(Temp);
                }
            }
        }


        public void SetPath(string filepath)
        {
            FilePath=filepath;
            img = Cv2.ImRead(FilePath);
        }

        public void ImShow()
        {
            Cv2.ImShow("Origin", img);
        }

        public void ImPointSetting()
        {
            GridPoint.Clear();//기본적으로 초기화 시킴
            img = Cv2.ImRead(FilePath);//이미지 초기화
            string winName = "Image Window";
            Cv2.NamedWindow(winName, WindowFlags.AutoSize);
            Cv2.ImShow("Image Window", img);
            Cv2.SetMouseCallback(winName, OnMouse);
        }

        private void OnMouse(MouseEventTypes @event, int x, int y, MouseEventFlags flags, IntPtr userdata)
        {
            if (@event == MouseEventTypes.LButtonDown)
            {
                // x, y 좌표 저장
                lastClickedPoint.X = x;
                lastClickedPoint.Y =y;
                Cv2.Circle(img, x, y, 5, Scalar.Red, -1);
                Cv2.ImShow("Image Window", img); //찍을때마다 다시줌
                GridPoint.Add(lastClickedPoint);//44
                if (GridPoint.Count >= GridCount)
                {
                    Cv2.DestroyAllWindows();
                    return;
                }
            }
        }

        public Bitmap GearGridWarpPerspective()
        {
            img = Cv2.ImRead(FilePath);//이미지 초기화

            Mat tempdst = new Mat();
            Point2f[] srcPoints = new Point2f[4];
            Point2f[] dstPoints = new Point2f[4] {
            new Point2f(0, 0),
            new Point2f(ResultWidth/10, 0),
            new Point2f(ResultWidth/10, ResultHeight/3),
            new Point2f(0, ResultHeight/3)
            };
            Mat hConnet = new Mat();
            Mat dst = new Mat();
            for (int i = 0; i < 30; i++)
            {
                srcPoints[0].X = GridPoint[i+ i / 10].X;
                srcPoints[0].Y = GridPoint[i + i / 10].Y;
                srcPoints[1].X = GridPoint[i + 1 + i / 10].X;
                srcPoints[1].Y = GridPoint[i + 1 + i / 10].Y;
                srcPoints[2].X = GridPoint[i + 12 + i / 10].X;
                srcPoints[2].Y = GridPoint[i + 12 + i / 10].Y;
                srcPoints[3].X = GridPoint[i + 11 + i / 10].X;
                srcPoints[3].Y = GridPoint[i + 11 + i / 10].Y;
                Mat matrix = Cv2.GetPerspectiveTransform(srcPoints, dstPoints);
                Cv2.WarpPerspective(img, tempdst, matrix, new OpenCvSharp.Size(ResultWidth / 10, ResultHeight / 3));
                //이후에 여기서 결점이 생긴지점의 위치 정보가 포함되어 있는지를 확인하고
                // 결점의 위치가 포함된 지점이라면 해당 매트릭스 정보와 몇번째 grid인지 같이 저장하고
                // grid와 매트릭스를 이용해서 캘리브레이션이 됐을 때의 결점 위치를 계산해 낼 수 있음
                if (i%10==0)
                {
                    hConnet = tempdst.Clone();
                }
                else {
                    Cv2.HConcat(new Mat[] { hConnet, tempdst }, hConnet);
                }

                if (i % 10 == 9)
                {
                    if (i == 9)
                    {
                        dst= hConnet.Clone();
                    }
                    else
                    {
                        Cv2.VConcat(new Mat[] { dst, hConnet }, dst);
                    }
                    hConnet.Dispose();
                    hConnet = new Mat();
                }
            }

            return OpenCvSharp.Extensions.BitmapConverter.ToBitmap(dst);
        }


    }
}

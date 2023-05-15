﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.Blob;
using System.IO;

namespace Labeling
{
    public partial class Form1 : Form
    {
        VideoCapture gCap;
        string fname;       // 파일의 이름을 저장할 변수
        int fileIndex = 0;  // 디렉토리 내의 파일 인덱스를 저장할 변수

        public Form1()
        {
            InitializeComponent();
        }

        private void btnReadFile_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)  // 파일읽기
            {
                fname = openFileDialog.FileName;
                Mat mat = new Mat(fname);
                picSrc.Image = mat.ToBitmap();
                lblTitleOfPicture.Text = Path.GetFileName(fname);
            }
        }

        private void btnNextFile_Click(object sender, EventArgs e)
        {
            if (fname != null)                                      // 파일을 읽은 적이 있으면
            {
                string path = Path.GetDirectoryName(fname);         // 해당 파일이 있는 디렉토리의 경로
                string[] files = Directory.GetFiles(path);          // 디렉토리 내의 모든 파일들의 파일명을 배열에 저장한다.

                if (fileIndex == files.Length - 1) fileIndex = -1;  // 만약 해당 파일이 디렉토리 마지막 파일이라면 다시 첫 번째 파일을 읽도록 fileIndex를 -1로 한다.
                
                fname = files[++fileIndex];                         // fname에 다음 파일의 이름을 저장한다.

                Mat mat = new Mat(fname);                   
                picSrc.Image = mat.ToBitmap();                      // picSrc에 다음 파일 사진을 출력한다.
                lblTitleOfPicture.Text = Path.GetFileName(fname);   // lblTitleofPicture에 파일명을 출력한다.
            }
        }

        private void btnWebCam_Click(object sender, EventArgs e)
        {
            if (timCam.Enabled == false)
            {
                int idxcam = 0;
                gCap = new VideoCapture(CaptureDevice.DShow, idxcam);

                // 필요하면 해상도 설정
                gCap.FrameWidth = 1280;
                gCap.FrameHeight = 1024;

                if (gCap.IsOpened() == false) return;

                timCam.Interval = 33;   //초당 30프레임 설정
                timCam.Enabled = true;
            }
            else
            {
                // 비활성
                timCam.Enabled = false;
                chkCam.Checked = false;
                if (gCap != null) gCap.Dispose();
            }
        }

        private void timCam_Tick(object sender, EventArgs e)
        {
            //gCap.Grab();  <- 내부 메모리에 먼저 저장할 필요가 있을 때 사용
            Mat mat = gCap.RetrieveMat();
            picSrc.Image = mat.ToBitmap();

            // 깜박거림 표시
            chkCam.Checked = !chkCam.Checked;

            // Garbage Collector
            //GC.Collect(); // 수행하면 메모리 쌓이지 않음. CPU는 UP
        }

        private void btnToGray_Click(object sender, EventArgs e)
        {
            if (picSrc.Image == null) return;

            // picSrc의 Image를 추출하여 matSrc에 저장
            Bitmap bmp = picSrc.Image as Bitmap;
            Mat matSrc = BitmapConverter.ToMat(bmp);

            // (OpenCV 함수를 이용하여) matSrc를 Gray로 변환
            Mat matGray = matSrc.CvtColor(ColorConversionCodes.BGR2GRAY);
            picGray.Image = matGray.ToBitmap();
        }

        private void btnToBin_Click(object sender, EventArgs e)
        {
            if (picGray.Image == null) return;

            // picGray의 Image를 추출하여 matGray에 저장
            Bitmap bmp = picGray.Image as Bitmap;
            Mat matGray = BitmapConverter.ToMat(bmp);

            // (OpenCV 함수를 이용하여) matGray를 Binary로 변환
            if (radOtus.Checked)
            {
                Mat matBin = matGray.Threshold(0, 255, ThresholdTypes.Otsu);
                picBin.Image = matBin.ToBitmap();
                
            }
            else if (radBin.Checked)
            {
                double thresh = hscThreshold.Value;
                Mat matBin = matGray.Threshold(thresh, 255, ThresholdTypes.Binary);
                picBin.Image = matBin.ToBitmap();
            }
        }

        private void hscThreshold_Scroll(object sender, ScrollEventArgs e)
        {
            lblThreshold.Text = Convert.ToString(hscThreshold.Value);
            btnToBin.PerformClick();
        }

        private void radOtus_CheckedChanged(object sender, EventArgs e)
        {
            btnToBin.PerformClick();
        }

        private void radBin_CheckedChanged(object sender, EventArgs e)
        {
            btnToBin.PerformClick();
        }

        private void btnEdge_Click(object sender, EventArgs e)
        {
            if (picGray.Image == null) return;

            // picGray의 Image를 추출하여 matGray에 저장
            Bitmap bmp = picGray.Image as Bitmap;
            Mat matGray = BitmapConverter.ToMat(bmp);

            // (OpenCV 함수를 이용하여) Canny Edge 영상 얻기
            int threshold = hscThreshold.Value;
            Mat matEdge = matGray.Canny(threshold, 255);
            picResult.Image = matEdge.ToBitmap();
        }

        private void btnLabelingCV_Click(object sender, EventArgs e)
        {
            if (picBin.Image == null) return;

            // picBin의 Image를 추출하여 matBin에 저장
            Bitmap bmp = picBin.Image as Bitmap;
            Mat matBin = BitmapConverter.ToMat(bmp);

            // (kLabeling 을 이용하여) matBin을 Labeling
            DateTime stime = DateTime.Now;

            Mat matResult;
            CvBlob[] blobArr = LabelingCV.FindBlobs(matBin, out matResult);
            int nblob = blobArr.Length;

            double dtime = Util.TimeInSeconds(stime);

            // 결과 그림 표시
            picResult.Image = matResult.ToBitmap();

            // 결과 텍스트창에 표시
            int area;
            double xcen, ycen;
            txtLabelingResult.Text = "라벨링시간(초)= " + string.Format("{0:##0.000}", dtime) + "\r\n";

            for (int i = 0; i < nblob; i++)
            {
                LabelingCV.getAreaCenter(blobArr[i], out area, out xcen, out ycen);
                txtLabelingResult.Text += "라벨번호= " + Convert.ToString(i + 1).PadLeft(2) + "  " +
                                        "면적= " + Convert.ToString(area).PadLeft(5) + "  " +
                                        "중심= " + string.Format("{0:##0.00}", xcen) + ", " +
                                        String.Format("{0:##0.00}", ycen) + "\r\n";
            }
        }

        private void btnLabelingK_Click(object sender, EventArgs e)
        {
            if (picBin.Image == null) return;

            // picBin의 Image를 추출하여 matBin에 저장
            Bitmap bmp = picBin.Image as Bitmap;
            Mat matBin = BitmapConverter.ToMat(bmp);

            // (kLabeling 을 이용하여) matBin을 Labeling
            DateTime stime = DateTime.Now;

            bool isObjWhite = true;
            Mat[] matLabels = LabelingK.getLabels(matBin, 0, isObjWhite);
            int nlabel = matLabels.Length;

            double dtime = Util.TimeInSeconds(stime);

            // 결과 그림 표시
            picResult.Image = picBin.Image;
            Application.DoEvents();     // 윈도그림 나타나게하기 위해

            // 결과 텍스트창에 표시
            int area;
            double xcen, ycen;
            txtLabelingResult.Text = "라벨링시간(초)= " + string.Format("{0:##0.000}", dtime) + "\r\n";

            for (int i = 0; i < nlabel; i++)
            {
                LabelingK.getAreaCenter(matLabels[i], isObjWhite, out area, out xcen, out ycen);
                txtLabelingResult.Text += "라벨번호= " + Convert.ToString(i + 1).PadLeft(2) + "  " +
                                        "면적= " + Convert.ToString(area).PadLeft(5) + "  " +
                                        "중심= " + string.Format("{0:##0.00}", xcen) + ", " +
                                        String.Format("{0:##0.00}", ycen) + "\r\n";

                Graphics grp = picResult.CreateGraphics();
                grp.DrawLine(new Pen(Color.Yellow), (float)xcen - 5, (float)ycen, (float)xcen + 5, (float)ycen);
                grp.DrawLine(new Pen(Color.Yellow), (float)xcen, (float)ycen - 5, (float)xcen, (float)ycen + 5);
            }
        }

        private void btnLabelingCoin_Click(object sender, EventArgs e)  // 동전을 라벨링하여 금액의 합을 계산하는 함수
        {
            if (picBin.Image == null) return;

            // picBin의 Image를 추출하여 matBin에 저장
            Bitmap bmp = picBin.Image as Bitmap;
            Mat matBin = BitmapConverter.ToMat(bmp);

            // (CVLabeling 을 이용하여) matBin을 Labeling
            DateTime stime = DateTime.Now;

            Mat matResult;
            CvBlob[] blobArr = LabelingCV.FindBlobs(matBin, out matResult);
            int nblob = blobArr.Length;

            double dtime = Util.TimeInSeconds(stime);

            // 결과 그림 표시
            picResult.Image = matResult.ToBitmap();

            int area;
            double xcen, ycen;

            // 각 동전의 개수를 저장할 변수
            int countCoin500 = 0; 
            int countCoin100 = 0;
            int countCoin50 = 0;
            int countCoin10 = 0;

            // 각 동전의 면적 값을 저장하는 상수
            const int coinArea500 = 250000;
            const int coinArea100 = 200000;
            const int coinArea50 = 150000;
            const int coinArea10 = 10000;

            int coinSum = 0;    // 동전들의 금액 합을 저장할 변수

            for (int i = 0; i < nblob; i++)
            {
                LabelingCV.getAreaCenter(blobArr[i], out area, out xcen, out ycen);

                // 넓이 조건에 따라 동전을 분류한다.
                if (area > coinArea500) countCoin500++;
                else if (area > coinArea100) countCoin100++;
                else if (area > coinArea50) countCoin50++;
                else if (area > coinArea10) countCoin10++;

            }

            coinSum = countCoin500 * 500 + countCoin100 * 100 + countCoin50 * 50 + countCoin10 * 10;    // 각 동전의 개수만큼 금액을 계산하여 coinSum에 더한다

            // 결과 텍스트창에 표시
            txtLabelingResult.Text  = "500원 동전 개수 : " + Convert.ToString(countCoin500) + "개\r\n";
            txtLabelingResult.Text += "100원 동전 개수 : " + Convert.ToString(countCoin100) + "개\r\n";
            txtLabelingResult.Text += "50원 동전 개수 : " + Convert.ToString(countCoin50) + "개\r\n";
            txtLabelingResult.Text += "10원 동전 개수 : " + Convert.ToString(countCoin10) + "개\r\n";
            txtLabelingResult.Text += "금액의 합 : " + Convert.ToString(coinSum) + "원\r\n";
        }
    }
}

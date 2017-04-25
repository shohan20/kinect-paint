//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
namespace Microsoft.Samples.Kinect.ColorBasics
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using System.Collections.Generic;
    using System.Windows.Controls;
    using System.Runtime.InteropServices;
    using System.Linq;
    using System.Windows.Shapes;


    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Reader for color frames
        /// </summary>
        private ColorFrameReader colorFrameReader = null;
        private BodyFrameReader bodyFrameReader = null;
        private int bodyIndex;
        int fcolor = 1;
        private Body[] bodies = null;
        Boolean fpaint = true;
        private DrawingImage imageSource;
        /// <summary>
        /// Bitmap to display
        /// </summary>
        private WriteableBitmap colorBitmap = null;
        private byte[] pixels = null;
        Boolean fgpoly = true;
        Canvas dcpaint;
        Polyline dppaint;
        Ellipse fell;
        private Boolean cirf=false,ellf=false,recf=false,squrf=false;
        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;
        private DrawingGroup drawingGroup;
        double temp = 0;
        private FrameDescription colorFrameDescription = null;
        
        //Runtime runtime = Runtime.Kinects[0];

        [DllImport("user32")]

        public static extern int SetCursorPos(int x, int y);
        private const int MOUSEEVENTF_MOVE = 0x0001;
        private const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const int MOUSEEVENTF_LEFTUP = 0x0004;
        private const int MOUSEVENTF_RIGHTDOWN = 0x008;

        [DllImport("user32.dll",
            CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]

        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            // get the kinectSensor object
            this.kinectSensor = KinectSensor.GetDefault();
            if (kinectSensor != null)
            {
                // open the reader for the color frames
                this.colorFrameReader = this.kinectSensor.ColorFrameSource.OpenReader();

                // wire handler for frame arrival
                this.colorFrameReader.FrameArrived += this.ColorReader_FrameArrived;

                this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();
                bodyFrameReader.FrameArrived += this.BodyReader_FrameArrived;

            
                // create the colorFrameDescription from the ColorFrameSource using Bgra format
                colorFrameDescription = this.kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
                FrameDescription depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;
                pixels = new byte[colorFrameDescription.Width * colorFrameDescription.Height * 4];
                // create the bitmap to display
                this.colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
               
                // set IsAvailableChanged event notifier
                this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

                // open the sensor
                this.kinectSensor.Open();

                // set the status text
                this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                                : Properties.Resources.NoSensorStatusText;
                drawingGroup = new DrawingGroup();
                this.imageSource = new DrawingImage(this.drawingGroup);
                // use the window object as the view model in this simple example
                this.DataContext = this;
                // initialize the components (controls) of the window
                this.InitializeComponent();
                //camera.Source = colorBitmap;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Console.Write("clicked\n");
        }

        public ImageSource ImageSource
        {
            get
            {
                return this.imageSource;
            }
        }
        private void newpoly()
        {
            dcpaint = new Canvas();
            dppaint = new Polyline();
            dppaint.StrokeThickness = 10;
            if (fcolor == 0)
                dppaint.Stroke = Brushes.Black;
            else if (fcolor == 1)
                dppaint.Stroke = Brushes.Red;
            else if (fcolor == 2)
                dppaint.Stroke = Brushes.Green;
            else if (fcolor == 3)
                dppaint.Stroke = Brushes.Purple;
            else if (fcolor == 4)
                dppaint.Stroke = Brushes.Yellow;
            else if (fcolor == 5)
                dppaint.Stroke = Brushes.Blue;
            else if (fcolor == 6)
            {
                dppaint.Stroke = Brushes.White;
                dppaint.StrokeThickness = 30;
            }
            
            dcpaint.Children.Add(dppaint);
            roo.Children.Add(dcpaint);
            
            fpoint.Fill = dppaint.Stroke;
            if (fcolor == 6)
                fpoint.Fill = Brushes.Gray;
        }
        private void BodyReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        { 

            bool dataReceived = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    
                   
                    this.bodies = new Body[bodyFrame.BodyCount];
                    bodyFrame.GetAndRefreshBodyData(bodies);
                    //  Console.WriteLine(canvas.Width + " " + canvas.Height);
                    Body body = bodies.Where(b => b.IsTracked).FirstOrDefault();

                    if (body != null)
                    {
                        using (DrawingContext dc = this.drawingGroup.Open())
                        {

                            Joint handRight = body.Joints[JointType.HandTipRight];
                            Joint handLeft = body.Joints[JointType.HandLeft];
                            CameraSpacePoint handRightPosition = handRight.Position;
                            ColorSpacePoint handRightPoint = this.kinectSensor.CoordinateMapper.MapCameraPointToColorSpace(handRightPosition);
                            CameraSpacePoint handLeftPosition = handLeft.Position;
                            ColorSpacePoint handLeftPoint = this.kinectSensor.CoordinateMapper.MapCameraPointToColorSpace(handLeftPosition);
                            /*Microsoft.Research.Kinect.Nui.Vector vector = new Microsoft.Research.Kinect.Nui.Vector();
                             vector.X = ScaleVector(1024, handright.Position.X);
                             vector.Y = ScaleVector(800, -handright.Position.Y);
                             vector.Z = handright.Position.Z;

                             handright.Position = vector;

                             int topofscreen;
                             int leftofscreen;

                            leftofscreen = Convert.ToInt32(handRightPoint.X);
                            topofscreen = Convert.ToInt32(handRightPoint.Y); 

                            DepthSpacePoint handPt = kinectSensor.CoordinateMapper.MapCameraPointToDepthSpace(handRightPosition);

                            Point relativePoint = new Point (handRightPoint.X-350, handRightPoint.Y-120 );

                            SetCursorPos((int)relativePoint.X, (int)relativePoint.Y);

                            */
                            //SetCursorPos(leftofscreen, topofscreen);
                            if (fpaint == true)
                            {
                                double x = handRightPoint.X;
                                double y = handRightPoint.Y;
                                
                                if ( (body.HandRightState == HandState.Closed /*|| body.HandRightState==HandState.Lasso */|| body.HandRightState == HandState.Unknown))
                                {
                                    fgpoly = true;
                                    Console.Write(x + " " + y + "\n");
                                    if (x <= (Canvas.GetLeft(Black) + Black.Width) && x >= Canvas.GetLeft(Black) && y <= (Canvas.GetTop(Black) + Black.Height) && y >= Canvas.GetTop(Black))
                                    {
                                        
                                        Console.Write(fcolor+"\n");
                                        fcolor = 0;
                                        newpoly();
                                    }
                                    else if (x <= (Canvas.GetLeft(Red) + Red.Width) && x >= Canvas.GetLeft(Red) && y <= (Canvas.GetTop(Red) + Red.Height) && y >= Canvas.GetTop(Red))
                                    {

                                        fcolor = 1;
                                        newpoly();
                                    }
                                    else if (x <= (Canvas.GetLeft(Green) + Green.Width) && x >= Canvas.GetLeft(Green) && y <= (Canvas.GetTop(Green) + Green.Height) && y >= Canvas.GetTop(Green))
                                    {
                                        fcolor = 2;
                                        newpoly();
                                    }
                                    else if (x <= (Canvas.GetLeft(Purple) + Purple.Width) && x >= Canvas.GetLeft(Purple) && y <= (Canvas.GetTop(Purple) + Purple.Height) && y >= Canvas.GetTop(Purple))
                                    {
                                        fcolor = 3;
                                        newpoly();
                                    }
                                    else if (x <= (Canvas.GetLeft(Yellow) + Yellow.Width) && x >= Canvas.GetLeft(Yellow) && y <= (Canvas.GetTop(Yellow) + Yellow.Height) && y >= Canvas.GetTop(Yellow))
                                    {
                                        fcolor = 4;
                                        newpoly();
                                    }
                                    else if (x <= (Canvas.GetLeft(Blue) + Blue.Width) && x >= Canvas.GetLeft(Blue) && y <= (Canvas.GetTop(Blue) + Blue.Height) && y >= Canvas.GetTop(Blue))
                                    {
                                        fcolor = 5;
                                        newpoly();
                                    }
                                    else if (x <= (Canvas.GetLeft(eraser) + eraser.Width) && x >= Canvas.GetLeft(eraser) && y <= (Canvas.GetTop(eraser) + eraser.Height) && y >= Canvas.GetTop(eraser))
                                    {
                                        fcolor = 6;
                                        newpoly();
                                    }
                                    else if (x <= 1779)
                                    {
                                        
                                        if(dppaint!=null)   
                                        dppaint.Points.Add(new Point { X = x, Y = y });
                                    }
                                }
                                else if(handRight.TrackingState != TrackingState.NotTracked && body.HandRightState != HandState.Closed)
                                {
                                    if (fgpoly == true)
                                    {
                                        newpoly();
                                        
                                        fgpoly = false;
                                    }
                                }  
                                   
                                
                                if (handRight.TrackingState != TrackingState.NotTracked)
                                {
                                    if (!double.IsInfinity(x - (fpoint.Width / 2.0)) && !double.IsInfinity(y - (fpoint.Height / 2.0)))
                                    {
                                        fpoint.Visibility = Visibility.Visible;
                                        Canvas.SetLeft(fpoint, x - (fpoint.Width / 2.0));
                                        Canvas.SetTop(fpoint, y - (fpoint.Height / 2.0));
                                        //dc.DrawEllipse(paint.Stroke, null, new Point(handRightPoint.X, handRightPoint.Y), 10, 10);
                                        //img.PointToScreen(new Point(handRightPoint.X, handRightPoint.Y));
                                    }
                                  }
                               
                                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.colorFrameDescription.Width, this.colorFrameDescription.Height));
                            }
                            else
                            {

                                Point right = new Point(handRightPoint.X, handRightPoint.Y);
                                Point left = new Point(handLeftPoint.X, handLeftPoint.Y);
                                double distance = Point.Subtract(left, right).Length;

                                // if(handRight.TrackingState != TrackingState.NotTracked && Button.)
                                if (distance <= 200 && handLeft.TrackingState != TrackingState.NotTracked && handRight.TrackingState != TrackingState.NotTracked && body.HandLeftState == HandState.Closed && body.HandRightState != HandState.Closed)
                                {

                                    // Console.Write(x + " " + Canvas.GetLeft(circle) + "\n");
                                    // if (!float.IsInfinity(x) && !float.IsInfinity(y))
                                    //{

                                    // DRAW!
                                    //trail.Points.Add(new Point { X = x, Y = y });

                                    // right = new Point(handRightPoint.X, handRightPoint.Y);
                                    // left = new Point(handLeftPoint.X, handLeftPoint.Y);
                                    //distance = Point.Subtract(left, right).Length;


                                    if (((Canvas.GetLeft(circle) + circle.Width) - left.X) <= circle.Width + 100 && (right.X - (Canvas.GetLeft(circle))) <= circle.Width + 100 && Math.Abs((Canvas.GetTop(circle) + circle.Height / 2.0) - left.Y) <= circle.Height / 2.0 && Math.Abs(right.Y - (Canvas.GetTop(circle) + circle.Height / 2.0)) <= circle.Height / 2.0 || cirf == true)
                                    {
                                        cirf = true;
                                        if (temp != 0)
                                        {
                                            if (circle.Width + (temp - distance) >= 10)
                                                circle.Width += (temp - distance);
                                            if (circle.Height + (temp - distance) >= 10)
                                                circle.Height += (temp - distance);
                                        }
                                        temp = distance;

                                    }
                                    else if (((Canvas.GetLeft(ellipse) + ellipse.Width) - left.X) <= ellipse.Width + 100 && (right.X - (Canvas.GetLeft(ellipse) + ellipse.Width)) <= ellipse.Width + 100 && Math.Abs((Canvas.GetTop(ellipse) + ellipse.Height / 2.0) - left.Y) <= ellipse.Height / 2.0 && Math.Abs(right.Y - (Canvas.GetTop(ellipse) + ellipse.Height / 2.0)) <= circle.Height / 2.0 || ellf == true)
                                    {
                                        ellf = true;
                                        if (temp != 0)
                                        {
                                            if (ellipse.Width + (temp - distance) >= 10)
                                                ellipse.Width += (temp - distance);
                                            if (ellipse.Height + (temp - distance) >= 10)
                                                ellipse.Height += (temp - distance);
                                        }
                                        temp = distance;

                                    }
                                    else if (((Canvas.GetLeft(rec) + rec.Width) - left.X) <= rec.Width + 100 && (right.X - (Canvas.GetLeft(rec) + rec.Width)) <= rec.Width + 100 && Math.Abs((Canvas.GetTop(rec) + rec.Height / 2.0) - left.Y) <= rec.Height / 2.0 && Math.Abs(right.Y - (Canvas.GetTop(rec) + rec.Height / 2.0)) <= rec.Height / 2.0 || recf == true)
                                    {
                                        recf = true;
                                        if (temp != 0)
                                        {
                                            if (rec.Width + (temp - distance) >= 10)
                                                rec.Width += (temp - distance);
                                            if (rec.Height + (temp - distance) >= 10)
                                                rec.Height += (temp - distance);
                                        }
                                        temp = distance;

                                    }
                                    else if (((Canvas.GetLeft(square) + square.Width) - left.X) <= square.Width + 100 && (right.X - (Canvas.GetLeft(square) + square.Width)) <= square.Width + 100 && Math.Abs((Canvas.GetTop(square) + square.Height / 2.0) - left.Y) <= square.Height / 2.0 && Math.Abs(right.Y - (Canvas.GetTop(square) + square.Height / 2.0)) <= square.Height / 2.0 || squrf == true)
                                    {
                                        squrf = true;
                                        if (temp != 0)
                                        {
                                            if (square.Width + (temp - distance) >= 10)
                                                square.Width += (temp - distance);
                                            if (square.Height + (temp - distance) >= 10)
                                                square.Height += (temp - distance);
                                        }
                                        temp = distance;

                                    }


                                }

                                else if (handRight.TrackingState != TrackingState.NotTracked && body.HandRightState == HandState.Closed)
                                {

                                    float x = handRightPoint.X;
                                    float y = handRightPoint.Y;
                                    Console.Write(x + " " + Canvas.GetLeft(circle) + "\n");
                                    // if (!float.IsInfinity(x) && !float.IsInfinity(y))
                                    //{

                                    // DRAW!
                                    //trail.Points.Add(new Point { X = x, Y = y });

                                    if ((x < (Canvas.GetLeft(circle) + circle.Width) && x > Canvas.GetLeft(circle) && y < (Canvas.GetTop(circle) + circle.Height) && y > Canvas.GetTop(circle)) || cirf == true)
                                    {
                                        cirf = true;
                                        Canvas.SetLeft(circle, x - (circle.Width / 2.0));
                                        Canvas.SetTop(circle, y - circle.Height / 2.0);
                                    }
                                    else if ((x < (Canvas.GetLeft(ellipse) + ellipse.Width) && x > Canvas.GetLeft(ellipse) && y < (Canvas.GetTop(ellipse) + ellipse.Height) && y > Canvas.GetTop(ellipse)) || ellf == true)
                                    {
                                        ellf = true;
                                        Canvas.SetLeft(ellipse, x - ellipse.Width / 2.0);
                                        Canvas.SetTop(ellipse, y - ellipse.Height);
                                    }
                                    else if ((x < (Canvas.GetLeft(rec) + rec.Width) && x > Canvas.GetLeft(rec) && y < (Canvas.GetTop(rec) + rec.Height) && y > Canvas.GetTop(rec)) || recf == true)
                                    {
                                        recf = true;
                                        Canvas.SetLeft(rec, x - rec.Width / 2.0);
                                        Canvas.SetTop(rec, y - rec.Height / 2.0);
                                    }
                                    else if ((x < (Canvas.GetLeft(square) + square.Width) && x > Canvas.GetLeft(square) && y < (Canvas.GetTop(square) + square.Height) && y > Canvas.GetTop(square)) || squrf == true)
                                    {
                                        squrf = true;
                                        Canvas.SetLeft(square, x - square.Width / 2.0);
                                        Canvas.SetTop(square, y - square.Height);
                                    }
                                    //}
                                }
                                else
                                {
                                    cirf = squrf = recf = ellf = false;
                                    temp = 0;
                                }
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (this.colorFrameReader != null)
            {
                // ColorFrameReder is IDisposable
                this.colorFrameReader.Dispose();
                this.colorFrameReader = null;
            }

            if(this.bodyFrameReader != null)
            {
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        /// <summary>
        /// Handles the color frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>

        private void ColorReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            using (var frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    frame.CopyConvertedFrameDataToArray(pixels, ColorImageFormat.Bgra);

                    colorBitmap.Lock();
                    Marshal.Copy(pixels, 0, colorBitmap.BackBuffer, pixels.Length);
                    colorBitmap.AddDirtyRect(new Int32Rect(0, 0, colorFrameDescription.Width, colorFrameDescription.Height));
                    colorBitmap.Unlock();
                }
            }
        }

        private void Reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            // ColorFrame is IDisposable
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {
                        this.colorBitmap.Lock();

                        // verify data and write the new color frame data to the display bitmap
                        if ((colorFrameDescription.Width == this.colorBitmap.PixelWidth) && (colorFrameDescription.Height == this.colorBitmap.PixelHeight))
                        {
                            colorFrame.CopyConvertedFrameDataToIntPtr(
                                this.colorBitmap.BackBuffer,
                                (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                ColorImageFormat.Bgra);

                            this.colorBitmap.AddDirtyRect(new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight));
                        }
                        Console.Write("oka\n");
                        this.colorBitmap.Unlock();
                    }
                }
            }
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }
    }
}

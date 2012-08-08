using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;


namespace AviPlayer_PictureBox {

    public partial class Form1 : Form {

        IntPtr capture;
        Timer myTimer;
        MovieInfo movieInfo;

        delegate void MovieHandler(object sender, MovieEvent e);
        event MovieHandler MovieHandlers;

        public Form1() {
            InitializeComponent();
            this.MovieHandlers += new MovieHandler(this.handler);
        }

        private void button1_Click(object sender, EventArgs e) {
            var dialog = new OpenFileDialog();
            dialog.Filter = "视频(*.avi)|*.avi";
            if (dialog.ShowDialog() == DialogResult.OK) {
                var file = dialog.FileName;
                capture = CvInvoke.cvCreateFileCapture(file);

                movieInfo = new MovieInfo(file, capture);
                trackBar.SetRange(0, movieInfo.frameCount);
                if (myTimer != null) {
                    myTimer.Stop();
                    myTimer.Dispose();
                }
                myTimer = new Timer();
                myTimer.Interval = 1000 / Convert.ToInt32(movieInfo.fps);
                myTimer.Tick += new EventHandler(MyTimer_Tick);

                if (MovieHandlers != null) {
                    MovieHandlers(this, new MovieEvent(MovieEvent.State.NewMovie));
                }

                myTimer.Start();
            }
        }

        private void MyTimer_Tick(object sender, EventArgs e) {
            int p = movieInfo.currentFrame + 1;
            if (p >= movieInfo.frameCount) {
                myTimer.Stop();
                renderFrame(0);
                if (MovieHandlers != null) {
                    MovieHandlers(this, new MovieEvent(MovieEvent.State.Stopped));
                }
                return;
            }
            renderFrame(p);
            if (MovieHandlers != null) {
                MovieHandlers(this, new MovieEvent(MovieEvent.State.Playing));
            }
        }

        private void button3_Click(object sender, EventArgs e) {
            if (myTimer == null) return;
            myTimer.Stop();
            renderFrame(0);
            if (MovieHandlers != null) {
                MovieHandlers(this, new MovieEvent(MovieEvent.State.Stopped));
            }
        }

        private void renderFrame(int p) {
            if (p < 0) {
                p = 0;
            } else if (p >= movieInfo.frameCount) {
                p = movieInfo.frameCount - 1;
            }
            CvInvoke.cvSetCaptureProperty(capture, Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_POS_FRAMES, p);
            var frame = CvInvoke.cvQueryFrame(capture);
            Image<Bgr, byte> dest = new Image<Bgr, byte>(movieInfo.width, movieInfo.height);
            CvInvoke.cvCopy(frame, dest, IntPtr.Zero);

            pictureBox1.Image = dest.ToBitmap();
            movieInfo.currentFrame = p;
        }


        struct MovieInfo {
            public String filename;
            public int frameCount;
            public int width;
            public int height;
            public int currentFrame;
            public int fps;
            public bool playing;
            public MovieInfo(String filename, IntPtr capture) {
                this.filename = filename;
                this.frameCount = Convert.ToInt32(CvInvoke.cvGetCaptureProperty(capture, Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_COUNT));
                this.width = Convert.ToInt32(CvInvoke.cvGetCaptureProperty(capture, Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_WIDTH));
                this.height = Convert.ToInt32(CvInvoke.cvGetCaptureProperty(capture, Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_HEIGHT));
                this.currentFrame = Convert.ToInt32(CvInvoke.cvGetCaptureProperty(capture, Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_POS_FRAMES));
                this.fps = Convert.ToInt32(CvInvoke.cvGetCaptureProperty(capture, Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FPS));
                this.playing = false;
            }
        }

        private void button2_Click(object sender, EventArgs e) {
            if (myTimer == null) return;
            if (myTimer.Enabled) {
                myTimer.Stop();
                MovieHandlers(this, new MovieEvent(MovieEvent.State.Paused));
            } else {
                myTimer.Start();
                MovieHandlers(this, new MovieEvent(MovieEvent.State.Started));
            }
        }

        class MovieEvent : EventArgs {
            public State EventState { get; set; }
            public enum State {
                NewMovie, Started, Stopped, Paused, Playing, Scroll, PrevNext
            }
            public MovieEvent(State state)
                : base() {
                this.EventState = state;
            }
        }

        private void button4_Click(object sender, EventArgs e) {
            renderFrame(movieInfo.currentFrame - 1);
            if (MovieHandlers != null) {
                MovieHandlers(this, new MovieEvent(MovieEvent.State.PrevNext));
            }
        }

        private void button5_Click(object sender, EventArgs e) {
            renderFrame(movieInfo.currentFrame + 1);
            if (MovieHandlers != null) {
                MovieHandlers(this, new MovieEvent(MovieEvent.State.PrevNext));
            }
        }

        private void handler(object sender, MovieEvent e) {
            switch (e.EventState) {
                case MovieEvent.State.NewMovie:
                    btnStart.Enabled = true;
                    btnStop.Enabled = true;
                    btnPrev.Enabled = true;
                    btnNext.Enabled = true;
                    btnStart.Text = "暂停";
                    break;
                case MovieEvent.State.Paused:
                    btnStart.Enabled = true;
                    btnStop.Enabled = true;
                    btnPrev.Enabled = true;
                    btnNext.Enabled = true;
                    btnStart.Text = "开始";
                    break;
                case MovieEvent.State.Playing:
                    btnStart.Enabled = true;
                    btnStop.Enabled = true;
                    btnPrev.Enabled = true;
                    btnNext.Enabled = true;
                    btnStart.Text = "暂停";
                    trackBar.Value = movieInfo.currentFrame;
                    break;
                case MovieEvent.State.PrevNext:
                    btnStart.Enabled = true;
                    btnStop.Enabled = true;
                    btnPrev.Enabled = true;
                    btnNext.Enabled = true;
                    trackBar.Value = movieInfo.currentFrame;
                    break;
                case MovieEvent.State.Started:
                    btnStart.Enabled = true;
                    btnStop.Enabled = true;
                    btnPrev.Enabled = true;
                    btnNext.Enabled = true;
                    btnStart.Text = "暂停";
                    break;
                case MovieEvent.State.Stopped:
                    btnStart.Enabled = true;
                    btnStop.Enabled = false;
                    btnPrev.Enabled = false;
                    btnNext.Enabled = false;
                    btnStart.Text = "开始";
                    trackBar.Value = 0;
                    break;
                case MovieEvent.State.Scroll:
                    btnStart.Enabled = true;
                    btnStop.Enabled = true;
                    btnPrev.Enabled = true;
                    btnNext.Enabled = true;
                    break;
                default: break;
            }
        }

        private void trackBar_Scroll(object sender, EventArgs e) {
            renderFrame((sender as TrackBar).Value);
            if (this.MovieHandlers != null) {
                MovieHandlers(this, new MovieEvent(MovieEvent.State.Scroll));
            }
        }

    }

}

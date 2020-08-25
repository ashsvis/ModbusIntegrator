using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.ComponentModel;

namespace StatusControl
{
    public partial class StatusControl : Control
    {
        private const int side = 21;

        public StatusControl()
        {
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Size = new Size(side, side);
        }

        [Category("Appearance"), Description("The current value of the altitude anglepanel control."), DefaultValue(typeof(Size), "21; 21")]
        public new Size Size
        {
            get { return base.Size; }
            set { base.Size = value.IsEmpty ? new Size(side, side) : value; }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var gr = e.Graphics;
            using (var path = GetAreaPath())
            {
                var rect = path.GetBounds();
                using (var brush = new SolidBrush(State != null ?
                    ((bool)State ? LampColorOn : LampColorOff) : LampColorNone))
                {
                    gr.FillEllipse(brush, rect);
                    gr.DrawEllipse(SystemPens.WindowFrame, rect);
                }
                var offset = new PointF(rect.Width / 5.3f, rect.Height / 5.3f);
                var size = new SizeF(rect.Width / 4.5f, rect.Height / 4.5f);
                var blick = new RectangleF(offset, size);
                gr.FillEllipse(SystemBrushes.ButtonHighlight, blick);
            }
        }

        private bool? _state;

        public bool? State
        {
            get { return _state; }
            set
            {
                _state = value;
                Invalidate();
            }
        }

        private Color _lampColorOn = SystemColors.Control;

        public Color LampColorOn
        {
            get { return _lampColorOn; }
            set 
            { 
                _lampColorOn = value;
                Invalidate();
            }
        }

        private Color _lampColorOff = SystemColors.Control;

        public Color LampColorOff
        {
            get { return _lampColorOff; }
            set 
            { 
                _lampColorOff = value;
                Invalidate();
            }
        }

        private Color _lampColorNone = Color.Gray;

        public Color LampColorNone
        {
            get { return _lampColorNone; }
            set 
            { 
                _lampColorNone = value;
                Invalidate();
            }
        }

        private GraphicsPath GetAreaPath()
        {
            var path = new GraphicsPath();
            var rect = new Rectangle(Point.Empty, Size);
            rect.Width -= 1;
            rect.Height -= 1;
            path.AddEllipse(rect);
            return path;
        }

    }
}

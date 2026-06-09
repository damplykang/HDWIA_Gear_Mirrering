using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

public class TranslucentPanel : Panel
{
    private int opacity = 125; // 0(완전 투명) ~ 255(완전 불투명)
    [Browsable(true)]
    [Category("Appearance")]
    [Description("패널의 투명도를 설정합니다. (0 ~ 255)")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public int Opacity
    {
        get { return opacity; }
        set
        {
            if (value < 0) value = 0;
            if (value > 255) value = 255;
            opacity = value;
            this.Invalidate(); // 변경 시 패널을 다시 그림
        }
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= 0x00000020; // WS_EX_TRANSPARENT 스타일 적용 (투명 효과)
            return cp;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        // 지정한 투명도와 배경색(여기서는 검은색 배경의 반투명)으로 패널을 채움
        using (var brush = new SolidBrush(Color.FromArgb(this.opacity, Color.Black)))
        {
            e.Graphics.FillRectangle(brush, this.ClientRectangle);
        }

        base.OnPaint(e);
    }
}
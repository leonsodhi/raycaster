using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace raycaster
{
    public partial class frmRaycaster : Form
    {
        private CRayCaster m_rayCaster;

        private void updatePicture(Image img)
        {
            pictureBox1.Image = img;
        }

        public frmRaycaster()
        {
            InitializeComponent();
            System.Drawing.Bitmap canvas = new System.Drawing.Bitmap(pictureBox1.Width, pictureBox1.Height);

            m_rayCaster = new CRayCaster(canvas, this.BackColor);
            m_rayCaster.init();
            updatePicture(m_rayCaster.getImg());
        }

        private void frmRaycaster_Load(object sender, EventArgs e)
        {

        }

        private void frmRaycaster_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 'a')
            {                
                m_rayCaster.turnLeft();
            }
            if (e.KeyChar == 'd')
            {
                m_rayCaster.turnRight();             
            }
            if (e.KeyChar == 'w')
            {
                m_rayCaster.moveForwards();
            }
            if (e.KeyChar == 's')
            {
                m_rayCaster.moveBackwards();
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            btnStart.Enabled = false;
            this.Focus();

            while (true)
            {
                m_rayCaster.run();
                updatePicture(m_rayCaster.getImg());

                this.Refresh();
                Application.DoEvents();
                if (!this.Visible)
                    { break; }
                System.Threading.Thread.Sleep(50);
            }
        }
    }
}

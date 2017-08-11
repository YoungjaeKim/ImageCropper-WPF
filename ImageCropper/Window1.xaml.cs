using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;

namespace ImageCropper
{

    /// <summary>
    /// This window simply hosts a <see cref="UcImageCropper">UcImageCropper</see>
    /// control, and provides it with a new ImageUrl of the users picking
    /// </summary>
    public partial class Window1 : System.Windows.Window
    {
        public Window1()
        {
            InitializeComponent();
            rightImage.Margin = new Thickness(this.Width - (rightImage.Width + leftImage.Width + 30), 0, 0, 0);
        }

        #region Private methods
        /// <summary>
        /// Make sure the imageCropper logo stays to the right hand side of screen
        /// </summary>
        private void Window1_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            rightImage.Margin = new Thickness(this.ActualWidth - (rightImage.Width + leftImage.Width + 30), 0, 0, 0);
        }

        /// <summary>
        /// NOTE : This one method must be implemented to free up the temporary image created
        /// by the UcImageCropper
        /// </summary>
        private void Window1_Closed(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// get a file for the UcImageCropper to work with
        /// </summary>
        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();
            ofd.Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif";
            List<string> allowableFileTypes = new List<string>();
            allowableFileTypes.AddRange(new string[] { ".png", ".jpg",".jpeg", ".bmp",".gif" });
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (!ofd.FileName.Equals(String.Empty))
                {
                    FileInfo f = new FileInfo(ofd.FileName);
                    if (allowableFileTypes.Contains(f.Extension.ToLower()))
                    {
                        this.UcImageCropper.ImageUrl = f.FullName;
                    }
                    else
                    {
                        MessageBox.Show("Invalid file type");
                    }
                }
                else
                {
                    MessageBox.Show("You did pick a file to use");
                }
            }
        }
        #endregion
    }
}
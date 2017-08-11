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

#region Explanation of why this .NET3.0 app is using .NET2.0 Dlls
//For some very simple .NET niceties like being able to save a bitmap 
//to a filename I have had to use the System.Drawing .NET 2.0 Dll
//
//While this looks possible using something like the following :
//
//RenderTargetBitmap rtb = new RenderTargetBitmap((int)img.width,
//(int)img, 0, 0, PixelFormats.Default);
//rtb.Render(this.inkCanv);
//BmpBitmapEncoder encoder = new BmpBitmapEncoder();
//encoder.Frames.Add(BitmapFrame.Create(rtb));
//encoder.Save(file);
//file.Close();
//
//For this to work I would have needed to used a .NET 3.0 CroppedBitmap
//within the RenderTargetBitmap.Render() method. And as CroppedBitmap
//doesnt inherit from Visual this is not possible.
//
//So if anyone knows how to do this better in .NET 3.0 I am all ears
#endregion
using System.Drawing;
using System.Drawing.Drawing2D;

//Josh Smith excellent DragCanvas
using WPF.JoshSmith.Controls;

namespace ImageCropper
{

    /// <summary>
    /// Provides a simple Image cropping facility for a WPF image element,
    /// where the cropped area may be picked using a rubber band and moved
    /// by dragging the rubber band around the image. There is also a popup
    /// window from where the user may accept or reject the crop.
    /// </summary>
    public partial class UcImageCropper : System.Windows.Controls.UserControl
    {

        #region CropperStyle Dependancy property

        /// <summary>
        /// A DP for the Cropp Rectangle Style
        /// </summary>
        public Style CropperStyle
        {
            get { return (Style)GetValue(CropperStyleProperty); }
            set { SetValue(CropperStyleProperty, value); }
        }

        /// <summary>
        /// register the DP
        /// </summary>
        public static readonly DependencyProperty CropperStyleProperty =
            DependencyProperty.Register(
            "CropperStyle",
            typeof(Style),
            typeof(UcImageCropper),
            new UIPropertyMetadata(null, new PropertyChangedCallback(OnCropperStyleChanged)));

        /// <summary>
        /// The callback that actually changes the Style if one was provided
        /// </summary>
        /// <param name="depObj">UcImageCropper</param>
        /// <param name="e">The event args</param>
        static void OnCropperStyleChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            Style s = e.NewValue as Style;
            if (s != null)
            {
                UcImageCropper uc = (UcImageCropper)depObj;
                uc.selectCanvForImg.CropperStyle = s;
            }
        }
        #endregion

        #region Instance fields
        private string ImgUrl = "";
        private BitmapImage bmpSource = null;
        private SelectionCanvas selectCanvForImg = null;
        private DragCanvas dragCanvasForImg = null;
        private System.Windows.Controls.Image img = null;
        private Shape rubberBand;
        private double rubberBandLeft;
        private double rubberBandTop;
        private string tempFileName;
        private ContextMenu cmSelectionCanvas;
        private RoutedEventHandler cmSelectionCanvasRoutedEventHandler;
        private ContextMenu cmDragCanvas;
        private RoutedEventHandler cmDragCanvasRoutedEventHandler;
        private string fixedTempName = "temp";
        private long fixedTempIdx = 1;
        private double zoomFactor=1.0;
        #endregion

        #region Ctor
        public UcImageCropper()
        {
            InitializeComponent();

            //this.Unloaded += new RoutedEventHandler(UcImageCropper_Unloaded);
            selectCanvForImg = new SelectionCanvas();
            selectCanvForImg.CropImage += new RoutedEventHandler(selectCanvForImg_CropImage);
            dragCanvasForImg = new DragCanvas();
        }


        #endregion

        #region Public properties
        public string ImageUrl
        {
            get { return this.ImgUrl; }
            set
            {
                zoomFactor = 1.0;
                ImgUrl = value;
                grdCroppedImage.Visibility = Visibility.Hidden;
                createImageSource();
                createSelectionCanvas();
                //apply the default style if the user of this control didnt supply one
                if (CropperStyle == null)
                {
                    Style s = gridMain.TryFindResource("defaultCropperStyle") as Style;
                    if (s != null)
                    {
                        CropperStyle = s;
                    }
                }

            }
        }
        #endregion

        #region Private methods
        /// <summary>
        /// Deletes all occurences of previous unused temp files from the
        /// current temporary path
        /// </summary>
        /// <param name="tempPath">The temporary file path</param>
        /// <param name="fixedTempName">The file name part to search for</param>
        /// <param name="CurrentFixedTempIdx">The current temp file suffix</param>
        public void CleanUp(string tempPath, string fixedTempName, long CurrentFixedTempIdx)
        {
            //clean up the single temporary file created
            try
            {
                string filename = "";
                for (int i = 0; i < CurrentFixedTempIdx; i++)
                {
                    filename = tempPath + fixedTempName + i.ToString() + ".jpg";
                    File.Delete(filename);
			    }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Popup form Cancel clicked, so created the SelectionCanvas to start again
        /// </summary>
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            grdCroppedImage.Visibility = Visibility.Hidden;
            createSelectionCanvas();
        }

        /// <summary>
        /// Popup form Confirm clicked, so save the file to their desired location
        /// </summary>
        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            ImageUrl = tempFileName;
            grdCroppedImage.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// creates the selection canvas, where user can draw
        /// selection rectangle
        /// </summary>
        private void createSelectionCanvas()
        {
            createImageSource();
            selectCanvForImg.Width = bmpSource.Width * zoomFactor;
            selectCanvForImg.Height = bmpSource.Height * zoomFactor;
            selectCanvForImg.Children.Clear();
            selectCanvForImg.rubberBand = null;
            selectCanvForImg.Children.Add(img);
            //svForImg.Width = selectCanvForImg.Width;
            //svForImg.Height = selectCanvForImg.Height;
            svForImg.Content = selectCanvForImg;
            //createSelectionCanvasMenu(); // NOTE: Youngjae (2017-08-11 17:32:19): crop zoomed image gives bad image result. So remove this function.
        }

        /// <summary>
        /// Creates the selection canvas context menu
        /// </summary>
        private void createSelectionCanvasMenu()
        {
            cmSelectionCanvas = new ContextMenu();
            MenuItem miZoom25 = new MenuItem();
            miZoom25.Header = "Zoom 25%";
            miZoom25.Tag = "0.25";
            MenuItem miZoom50 = new MenuItem();
            miZoom50.Header = "Zoom 50%";
            miZoom50.Tag = "0.5";
            MenuItem miZoom100 = new MenuItem();
            miZoom100.Header = "Zoom 100%";
            miZoom100.Tag = "1.0";
            cmSelectionCanvas.Items.Add(miZoom25);
            cmSelectionCanvas.Items.Add(miZoom50);
            cmSelectionCanvas.Items.Add(miZoom100);
            cmSelectionCanvasRoutedEventHandler = new RoutedEventHandler(MenuSelectionCanvasOnClick);
            cmSelectionCanvas.AddHandler(MenuItem.ClickEvent, cmSelectionCanvasRoutedEventHandler);
            selectCanvForImg.ContextMenu = cmSelectionCanvas;
        }

        /// <summary>
        /// Handles the selction canvas context menu. Which will zoom the
        /// current image to either 25,50 or 100%
        /// </summary>
        private void MenuSelectionCanvasOnClick(object sender, RoutedEventArgs args)
        {
            MenuItem item = args.Source as MenuItem;
            zoomFactor = double.Parse(item.Tag.ToString());
            img.RenderTransform = new ScaleTransform(zoomFactor, zoomFactor, 0.5, 0.5);
            selectCanvForImg.Width = bmpSource.Width * zoomFactor;
            selectCanvForImg.Height = bmpSource.Height * zoomFactor;
            //svForImg.Width = selectCanvForImg.Width;
            //svForImg.Height = selectCanvForImg.Height;

        }

        /// <summary>
        /// Creates the Image source for the current canvas
        /// </summary>
        private void createImageSource()
        {
            bmpSource = new BitmapImage(new Uri(ImgUrl));
            img = new System.Windows.Controls.Image();
            img.Source = bmpSource;
            //if there was a Zoom Factor applied
            img.RenderTransform = new ScaleTransform(zoomFactor, zoomFactor, 0.5, 0.5);
        }

        /// <summary>
        /// creates the drag canvas, where user can drag the
        /// selection rectangle
        /// </summary>
        private void createDragCanvas()
        {
            dragCanvasForImg.Width = bmpSource.Width * zoomFactor;
            dragCanvasForImg.Height = bmpSource.Height * zoomFactor;
            //svForImg.Width = dragCanvasForImg.Width;
            //svForImg.Height = dragCanvasForImg.Height;
            createImageSource();
            createDragCanvasMenu();
            selectCanvForImg.Children.Remove(rubberBand);
            dragCanvasForImg.Children.Clear();
            dragCanvasForImg.Children.Add(img);
            dragCanvasForImg.Children.Add(rubberBand);
            svForImg.Content = dragCanvasForImg;
        }

        /// <summary>
        /// Creates the drag canvas context menu
        /// </summary>
        private void createDragCanvasMenu()
        {
            cmSelectionCanvas.RemoveHandler(MenuItem.ClickEvent, cmSelectionCanvasRoutedEventHandler);
            selectCanvForImg.ContextMenu = null;
            cmSelectionCanvas = null;
            cmDragCanvas = new ContextMenu();
            MenuItem miCancel = new MenuItem();
            miCancel.Header = "Cancel";
            MenuItem miSave = new MenuItem();
            miSave.Header = "Save";
            cmDragCanvas.Items.Add(miCancel);
            cmDragCanvas.Items.Add(miSave);
            cmDragCanvasRoutedEventHandler = new RoutedEventHandler(MenuDragCanvasOnClick);
            cmDragCanvas.AddHandler(MenuItem.ClickEvent, cmDragCanvasRoutedEventHandler);
            dragCanvasForImg.ContextMenu = cmDragCanvas;
        }

        /// <summary>
        /// Handles the selction drag context menu. Which allows user to cancel or save 
        /// the current croped area
        /// </summary>
        private void MenuDragCanvasOnClick(object sender, RoutedEventArgs args)
        {
            MenuItem item = args.Source as MenuItem;
            switch (item.Header.ToString())
            {
                case "Save":
                    SaveCroppedImage();
                    break;
                case "Cancel":
                    createSelectionCanvas();
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Raised by the <see cref="selectionCanvas">selectionCanvas</see>
        /// when the new crop shape (rectangle) has been drawn. This event
        /// then replaces the current selectionCanvas with a <see cref="DragCanvas">DragCanvas</see>
        /// which can then be used to drag the crop area around within a Canvas
        /// </summary>
        private void selectCanvForImg_CropImage(object sender, RoutedEventArgs e)
        {
            rubberBand = (Shape)selectCanvForImg.Children[1];
            createDragCanvas();
        }

        /// <summary>
        /// User cancelled out of the popup, so go back to showing original image
        /// </summary>
        private void lblExit_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            grdCroppedImage.Visibility = Visibility.Hidden;
            createSelectionCanvas();
        }

        /// <summary>
        /// Saves the cropped image area to a temp file, and shows a confirmation
        /// popup from where the user may accept or reject the cropped image.
        /// If they accept the new cropped image will be used as the new image source
        /// for the current canvas, if they reject the crop, the existing image will
        /// be used for the current canvas
        /// </summary>
        private void SaveCroppedImage()
        {
            if (popUpImage.Source!=null)
                popUpImage.Source = null; 

            try
            {
                rubberBandLeft = Canvas.GetLeft(rubberBand);
                rubberBandTop = Canvas.GetTop(rubberBand);
                //create a new .NET 2.0 bitmap (which allowing saving) based on the bound bitmap url
                using (System.Drawing.Bitmap source = new System.Drawing.Bitmap(ImgUrl))
                {
                    //create a new .NET 2.0 bitmap (which allowing saving) to store cropped image in, should be 
                    //same size as rubberBand element which is the size of the area of the original image we want to keep
                    using (System.Drawing.Bitmap target = new System.Drawing.Bitmap((int)rubberBand.Width, (int)rubberBand.Height))
                    {
                        //create a new destination rectange
                        System.Drawing.RectangleF recDest = new System.Drawing.RectangleF(0.0f, 0.0f, (float)target.Width, (float)target.Height);
                        //different resolution fix prior to cropping image
                        float hd = 1.0f / (target.HorizontalResolution / source.HorizontalResolution);
                        float vd = 1.0f / (target.VerticalResolution / source.VerticalResolution);
                        float hScale = 1.0f / (float)zoomFactor;
                        float vScale = 1.0f / (float)zoomFactor;
                        System.Drawing.RectangleF recSrc = new System.Drawing.RectangleF((hd * (float)rubberBandLeft) * hScale, (vd * (float)rubberBandTop) * vScale, (hd * (float)rubberBand.Width) * hScale, (vd * (float)rubberBand.Height) * vScale);
                        using (System.Drawing.Graphics gfx = System.Drawing.Graphics.FromImage(target))
                        {
                            gfx.DrawImage(source, recDest, recSrc, System.Drawing.GraphicsUnit.Pixel);
                        }
                        //create a new temporary file, and delete all old ones prior to this new temp file
                        //This is is a hack that I had to put in, due to GDI+ holding on to previous 
                        //file handles used by the Bitmap.Save() method the last time this method was run.
                        //This is a well known issue see http://support.microsoft.com/?id=814675 for example
                        tempFileName = System.IO.Path.GetTempPath();
                        if (fixedTempIdx > 2)
                            fixedTempIdx = 0;
                        else
                            ++fixedTempIdx;
                        //do the clean
                        CleanUp(tempFileName, fixedTempName, fixedTempIdx);
                        //Due to the so problem above, which believe you me I have tried and tried to resolve
                        //I have tried the following to fix this, incase anyone wants to try it
                        //1. Tried reading the image as a strea of bytes into a new bitmap image
                        //2. I have tried to use teh WPF BitmapImage.Create()
                        //3. I have used the trick where you use a 2nd Bitmap (GDI+) to be the newly saved
                        //   image
                        //
                        //None of these worked so I was forced into using a few temp files, and pointing the 
                        //cropped image to the last one, and makeing sure all others were deleted.
                        //Not ideal, so if anyone can fix it please this I would love to know. So let me know
                        tempFileName = tempFileName + fixedTempName + fixedTempIdx.ToString() + ".jpg";
                        target.Save(tempFileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                        //rewire up context menu
                        cmDragCanvas.RemoveHandler(MenuItem.ClickEvent, cmDragCanvasRoutedEventHandler);
                        dragCanvasForImg.ContextMenu = null;
                        cmDragCanvas = null;
                        //create popup BitmapImage
                        BitmapImage bmpPopup = new BitmapImage(new Uri(tempFileName));
                        popUpImage.Source = bmpPopup;
                        grdCroppedImage.Visibility = Visibility.Visible;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        #endregion
    }
}
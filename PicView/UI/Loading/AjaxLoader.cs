using PicView.UI.Animations;
using System;
using static PicView.UI.UserControls.UC;

namespace PicView.UI.Loading
{
    internal static class AjaxLoader
    {
        //// AjaxLoading
        ///// <summary>
        ///// Loads AjaxLoading and adds it to the window
        ///// </summary>
        //internal static void LoadAjaxLoading()
        //{
        //    ajaxLoading = new AjaxLoading
        //    {
        //        Focusable = false,
        //        Opacity = 0
        //    };

        //    mainWindow.bg.Children.Add(ajaxLoading);
        //}

        /// <summary>
        /// Start loading animation
        /// </summary>
        internal static void AjaxLoadingStart()
        {
            if (ajaxLoading.Opacity != 1)
            {
                AnimationHelper.Fade(ajaxLoading, 1, TimeSpan.FromSeconds(.2));
            }
        }

        /// <summary>
        /// End loading animation
        /// </summary>
        internal static void AjaxLoadingEnd()
        {
            AnimationHelper.Fade(ajaxLoading, 0, TimeSpan.FromSeconds(.2));
        }
    }
}
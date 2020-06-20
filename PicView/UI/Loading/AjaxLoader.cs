using PicView.UI.Animations;
using System;
using static PicView.UI.UserControls.UC;

namespace PicView.UI.Loading
{
    internal static class AjaxLoader
    {

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
            if (ajaxLoading.Opacity != 0)
            {
                AnimationHelper.Fade(ajaxLoading, 0, TimeSpan.FromSeconds(.2));
            }
        }
    }
}
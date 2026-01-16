using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiTestTree
{
    /// <summary>
    /// This sub-class gives a ContentPage to be awaited as modal page.
    /// </summary>
    public class AwaitableModalContentPage<T> : ContentPage
    {
        private TaskCompletionSource<T>? _tcs;

        /// <summary>
        /// Call, if simply a page shall be shown
        /// </summary>
        /// <param name="navigation"></param>
        /// <returns></returns>
        public Task<T> MauiShowPageAsync(INavigation navigation)
        {
            _tcs = new();
            navigation.PushModalAsync(this);
            return _tcs.Task;
        }

        /// <summary>
        /// Call this from inside the page to set the result.
        /// </summary>
        /// <param name="res"></param>
        protected void SetResult(T res)
        {
            _tcs?.TrySetResult(res);
        }

        /// <summary>
        /// Call this to perform a complete modal dialogue.
        /// </summary>
        public async Task<T> PerformModalDialog(INavigation navigation)
        {
            var res = await MauiShowPageAsync(navigation);
            await Navigation.PopModalAsync();
            return res;
        }
    }
}

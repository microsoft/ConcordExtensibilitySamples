// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.CallStack;

namespace HelloWorld
{
    /// <summary>
    /// Defines the two possible states the HelloWorld stack frame filter can be in.
    /// </summary>
    internal enum State
    {
        /// <summary>
        /// The initial state (state that we start out in). At this point, we
        /// haven't seen any stack frames.
        /// </summary>
        Initial,

        /// <summary>
        /// The state that we are in after we added the '[Hello World]' frame.
        /// </summary>
        HelloWorldFrameAdded
    };

    /// <summary>
    /// HelloWorldDataItem is an internal object used to hold the data which the HelloWorld 
    /// component associates with a DkmStackContext. In other words, this is a state-store which
    /// the hello world sample can use to hold data associated with a stack walk session.
    /// </summary>
    internal class HelloWordDataItem : DkmDataItem
    {
        // Object is created from GetInstance
        private HelloWordDataItem()
        {
        }

        public State State
        {
            get;
            set;
        }

        /// <summary>
        /// Returns the instance of HelloWorldDataItem associated with the input DkmStackContext
        /// object. If there is not currently an associated HelloWorldDataItem, a new data item
        /// will be created.
        /// </summary>
        /// <param name="context">Object to obtain the data item from</param>
        /// <returns>The associated data item</returns>
        public static HelloWordDataItem GetInstance(DkmStackContext context)
        {
            HelloWordDataItem item = GetExistingInstance(context);
            if (item != null)
                return item;

            item = new HelloWordDataItem();
            context.SetDataItem<HelloWordDataItem>(DkmDataCreationDisposition.CreateNew, item);

            return item;
        }

        /// <summary>
        /// Returns the instance of HelloWorldDataItem associated with the input DkmStackContext
        /// object. If there is not currently an associated HelloWorldDataItem, return null.
        /// </summary>
        /// <param name="context">Object to obtain the data item from</param>
        /// <returns>[OPTIONAL] The associated data item</returns>
        public static HelloWordDataItem GetExistingInstance(DkmStackContext context)
        {
            return context.GetDataItem<HelloWordDataItem>();
        }
    }
}

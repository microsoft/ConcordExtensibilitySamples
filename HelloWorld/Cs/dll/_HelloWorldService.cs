// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.Debugger.CallStack;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;

namespace HelloWorld
{
    /// <summary>
    /// The one and only public class in the sample. This implements the IDkmCallStackFilter
    /// interface, which is how the sample is called.
    /// 
    /// Note that the list of interfaces implemented is defined here, and in 
    /// HelloWorld.vsdconfigxml. Both lists need to be the same.
    /// </summary>
    public class HelloWorldService : IDkmCallStackFilter
    {
        #region IDkmCallStackFilter Members

        DkmStackWalkFrame[] IDkmCallStackFilter.FilterNextFrame(DkmStackContext stackContext, DkmStackWalkFrame input)
        {
            // The HelloWorld sample is a very simple debugger component which modified the call stack
            // so that there is a '[Hello World]' frame at the top of the call stack. All the frames
            // below this are left the same.

            if (input == null) // null input frame indicates the end of the call stack. This sample does nothing on end-of-stack.
                return null;

            // Get the HelloWorldDataItem which is associated with this stack walk. This
            // lets us keep data associated with this stack walk.
            HelloWordDataItem dataItem = HelloWordDataItem.GetInstance(stackContext);
            DkmStackWalkFrame[] frames;

            // Now use this data item to see if we are looking at the first (top-most) frame
            if (dataItem.State == State.Initial)
            {
                // On the top most frame, we want to return back two different frames. First 
                // we place the '[Hello World]' frame, and under that we put the input frame.

                frames = new DkmStackWalkFrame[2];

                // Create the hello world frame object, and stick it in the array
                frames[0] = DkmStackWalkFrame.Create(
                    stackContext.Thread,
                    null,                          // Annotated frame, so no instruction address
                    input.FrameBase,               // Use the same frame base as the input frame
                    0,                             // annoted frame uses zero bytes
                    DkmStackWalkFrameFlags.None,
                    "[Hello World]",               // Description of the frame which will appear in the call stack
                    null,                          // Annotated frame, so no registers
                    null
                    );

                // Add the input frame into the array as well
                frames[1] = input;

                // Update our state so that on the next frame we know not to add '[Hello World]' again.
                dataItem.State = State.HelloWorldFrameAdded;
            }
            else
            {
                // We have already added '[Hello World]' to this call stack, so just return
                // the input frame.

                frames = new DkmStackWalkFrame[1] { input };
            }

            return frames;
        }

        #endregion
    }
}

﻿using System;
using System.Windows;
using System.Windows.Input;

namespace Trigger.Generator
{
    public abstract class SmartRoutedCommand : RoutedCommand
    {

        public static bool GetIsCommandSink(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsCommandSinkProperty);
        }

        public static void SetIsCommandSink(DependencyObject obj, bool value)
        {
            obj.SetValue(IsCommandSinkProperty, value);
        }

        /// <summary>
        /// Represents the IsCommandSink attached property.  This is readonly.
        /// </summary>
        public static readonly DependencyProperty IsCommandSinkProperty =
         DependencyProperty.RegisterAttached(
         "IsCommandSink",
         typeof(bool),
         typeof(SmartRoutedCommand),
         new UIPropertyMetadata(false, OnIsCommandSinkChanged));

        /// <summary>
        /// Invoked when the IsCommandSink attached property is set on an element.
        /// </summary>
        /// <param name="depObj">The element on which the property was set.</param>
        /// <param name="e">Information about the property setting.</param>
        static void OnIsCommandSinkChanged(
          DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            bool isCommandSink = (bool)e.NewValue;

            UIElement sinkElem = depObj as UIElement;
            if (sinkElem == null)
                throw new ArgumentException("Target object must be a UIElement.");

            if (isCommandSink)
            {
                CommandManager.AddCanExecuteHandler(sinkElem, OnCanExecute);
                CommandManager.AddExecutedHandler(sinkElem, OnExecuted);
            }
            else
            {
                CommandManager.RemoveCanExecuteHandler(sinkElem, OnCanExecute);
                CommandManager.RemoveExecutedHandler(sinkElem, OnExecuted);
            }
        }

        static void OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            SmartRoutedCommand cmd = e.Command as SmartRoutedCommand;
            if (cmd != null)
            {
                e.CanExecute = cmd.CanExecuteCore(e.Parameter);
            }
        }

        static void OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            SmartRoutedCommand cmd = e.Command as SmartRoutedCommand;
            if (cmd != null)
            {
                cmd.ExecuteCore(e.Parameter);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Child classes override this method to provide logic which
        /// determines if the command can execute.  This method will 
        /// only be invoked if no element in the tree indicated that
        /// it can execute the command.
        /// </summary>
        /// <param name="parameter">The command parameter (optional).</param>
        /// <returns>True if the command can be executed, else false.</returns>
        protected abstract bool CanExecuteCore(object parameter);

        /// <summary>
        /// Child classes override this method to provide default 
        /// execution logic.  This method will only be invoked if
        /// CanExecuteCore returns true.
        /// </summary>
        /// <param name="parameter">The command parameter (optional).</param>
        protected abstract void ExecuteCore(object parameter);

    }
}

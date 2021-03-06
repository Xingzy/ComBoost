﻿using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Wodsoft.ComBoost.Business.Input;

namespace Wodsoft.ComBoost.Business.Controls.WorkPages
{
    public class Viewer<TEntity> : WorkPage where TEntity : EntityBase, new()
    {
        public EntityViewerViewModel<TEntity> ViewModel { get; private set; }
        private AppButtonPanel AppButtonPanel;
        private TreeView ParentTree;

        public ListView View { get; private set; }

        public Viewer()
        {
            EntityViewerViewModel<TEntity> viewModel = new EntityViewerViewModel<TEntity>(BussinessApplication.Current.ContextBuilder.GetContext<TEntity>());
            Initialize(viewModel);
        }

        public Viewer(EntityViewerViewModel<TEntity> viewModel)
        {
            Initialize(viewModel);
        }

        private TreeViewItem[] GetParentItems(EntityParentSelectorViewModel[] tree)
        {
            List<TreeViewItem> list = new List<TreeViewItem>();
            foreach (var node in tree)
            {
                TreeViewItem item = new TreeViewItem();
                item.Header = node.Name;
                item.Tag = node.Parents;
                item.IsExpanded = true;
                if (node.SubItems != null)
                    item.ItemsSource = GetParentItems(node.SubItems);
                list.Add(item);
            }
            return list.ToArray();
        }

        private void Initialize(EntityViewerViewModel<TEntity> viewModel)
        {
            if (viewModel == null)
                throw new ArgumentNullException("viewModel");

            Buttons = new System.Collections.ObjectModel.ObservableCollection<AppButton>();
            Buttons.CollectionChanged += Buttons_CollectionChanged;

            //Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Colours.xaml") });
            //Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Fonts.xaml") });
            //Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Controls.xaml") });
            //Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Accents/Blue.xaml") });
            //Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Accents/BaseLight.xaml") });
            //Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/Wodsoft.ComBoost.Business.Remote;component/Resources/Icons.xaml") });

            ViewModel = viewModel;
            viewModel.PropertyChanged += viewModel_PropertyChanged;
            viewModel.PageSize = 30;

            if (viewModel.AutoGenerateData && ViewModel.Items.Count == 0 && ViewModel.ParentModels == null)
                viewModel.ParentModels = EntityParentSelectorViewModel.CreateModel<TEntity>(BussinessApplication.Current.ContextBuilder);

            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(100, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(100, GridUnitType.Pixel) });
            if (viewModel.ParentModels != null && viewModel.ParentModels.Length != 0)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200, GridUnitType.Pixel) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                ParentTree = new TreeView();
                ParentTree.BorderThickness = new Thickness();
                TreeViewItem item = new TreeViewItem();
                item.Header = "全部";
                item.IsExpanded = true;
                item.ItemsSource = GetParentItems(viewModel.ParentModels);
                ParentTree.Items.Add(item);
                ParentTree.SelectedItemChanged += ParentTree_SelectedItemChanged;
                item.Margin = new Thickness(0, 0, 8, 0);
                grid.Children.Add(ParentTree);

                GridSplitter splitter = new GridSplitter();
                splitter.Width = 8;
                splitter.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                grid.Children.Add(splitter);
            }


            #region 列表显示
            View = new ListView();
            View.BorderThickness = new Thickness();
            if (viewModel.ParentModels != null && viewModel.ParentModels.Length != 0)
                Grid.SetColumn(View, 1);
            View.SelectionMode = SelectionMode.Single;
            GridView view = new GridView();
            view.AllowsColumnReorder = false;
            var entityType = typeof(TEntity);
            var viewBuilder = viewModel.ViewBuilder;
            var visableProperties = viewBuilder.VisableProperties;
            var hideProperties = viewBuilder.HideProperties;

            PropertyInfo[] properties;
            if (hideProperties == null)
                properties = visableProperties.Select(v => entityType.GetProperty(v)).Where(t => t != null).ToArray();
            else
                properties = visableProperties.Except(hideProperties).Select(v => entityType.GetProperty(v)).Where(t => t != null).ToArray();

            foreach (var property in properties)
            {
                GridViewColumn column = new GridViewColumn();
                var display = property.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
                if (display == null)
                    column.Header = property.Name;
                else
                    column.Header = new Label { Content = display.Name, ToolTip = display.Description };

                DataTemplate dt = new DataTemplate();
                FrameworkElementFactory fef = new FrameworkElementFactory(typeof(Label));
                Binding binding = new Binding();
                binding.Path = new PropertyPath(property.Name);
                fef.SetBinding(Label.ContentProperty, binding);
                dt.VisualTree = fef;
                column.CellTemplate = dt;
                view.Columns.Add(column);
            }
            View.View = view;

            viewModel.UpdateSource();
            View.DataContext = viewModel;
            View.SetBinding(DataGrid.ItemsSourceProperty, new Binding("ItemsSource"));

            grid.Children.Add(View);

            #endregion

            #region 按钮显示

            AppButtonPanel = new AppButtonPanel();
            Grid.SetRow(AppButtonPanel, 1);
            if (viewModel.ParentModels != null && viewModel.ParentModels.Length != 0)
                Grid.SetColumnSpan(AppButtonPanel, 2);
            grid.Children.Add(AppButtonPanel);

            AppButton back = new AppButton(new CustomCommand(null, Back));
            back.Text = "返回";
            back.Image = (Canvas)Resources["appbar_arrow_left"];
            AppButtonPanel.Items.Add(back);

            //AppButton firstPage = new AppButton(new CustomCommand(CanFirstPage, FirstPage));
            //firstPage.Text = "第一页";
            //firstPage.Image = (Canvas)Application.Current.Resources["appbar_navigate_previous"];
            //AppButtonPanel.Items.Add(firstPage);

            AppButton previousPage = new AppButton(new CustomCommand(CanPreviousPage, PreviousPage));
            previousPage.Text = "上一页";
            previousPage.Image = (Canvas)Resources["appbar_navigate_previous"];
            AppButtonPanel.Items.Add(previousPage);

            AppButton nextPage = new AppButton(new CustomCommand(CanNextPage, NextPage));
            nextPage.Text = "下一页";
            nextPage.Image = (Canvas)Resources["appbar_navigate_next"];
            AppButtonPanel.Items.Add(nextPage);

            if (viewModel.ViewBuilder.AllowedAdd)
            {
                AppButton add = new AppButton(new CustomCommand(CanAdd, Add));
                add.Text = "新增";
                add.Image = (Canvas)Resources["appbar_add"];
                AppButtonPanel.Items.Add(add);
            }

            if (viewModel.ViewBuilder.AllowedRemove)
            {
                AppButton remove = new AppItemButton(View, CanRemove, Remove);
                remove.Text = "删除";
                remove.Image = (Canvas)Resources["appbar_delete"];
                AppButtonPanel.Items.Add(remove);
            }

            if (viewModel.ViewBuilder.AllowedEdit)
            {
                AppButton edit = new AppItemButton(View, CanEdit, Edit);
                edit.Text = "编辑";
                edit.Image = (Canvas)Resources["appbar_edit"];
                AppButtonPanel.Items.Add(edit);
            }

            #endregion

            Content = grid;

            Loaded += Viewer_Loaded;
        }

        private void ParentTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (ParentTree.SelectedItem != null)
            {
                Frame.ShowLoading();
                TreeViewItem item = (TreeViewItem)ParentTree.SelectedItem;
                if (item.Tag == null)
                {
                    Thread thread = new Thread(() =>
                    {
                        ViewModel.Items.Clear();
                        foreach (var key in ViewModel.EntityContext.GetKeys())
                            ViewModel.Items.Add(key);
                        ViewModel.UpdateSource();
                        Dispatcher.Invoke(new Action(() => Frame.HideLoading()));
                    });
                    thread.Start();
                }
                else
                {
                    Guid[] parents = (Guid[])item.Tag;
                    Thread thread = new Thread(() =>
                    {
                        ViewModel.Items.Clear();
                        foreach (var key in ViewModel.EntityContext.InParent(parents))
                            ViewModel.Items.Add(key);
                        ViewModel.UpdateSource();
                        Dispatcher.Invoke(new Action(() => Frame.HideLoading()));
                    });
                    thread.Start();
                }
            }
        }

        private void Viewer_Loaded(object sender, RoutedEventArgs e)
        {
            if (Frame == null)
                return;
            Frame.ShowLoading();
            Thread thread = new Thread(() =>
            {
                if (ViewModel.Items.Count == 0 && ViewModel.AutoGenerateData)
                {
                    foreach (var key in ViewModel.EntityContext.GetKeys())
                        ViewModel.Items.Add(key);
                }
                ViewModel.UpdateSource();
                Dispatcher.Invoke(new Action(() => Frame.HideLoading()));
            });
            thread.Start();
        }

        private void viewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (AppButtonPanel != null)
                Dispatcher.Invoke(new Action(() => AppButtonPanel.UpdateCommand()));
        }

        #region 按钮事件

        private void Back(object sender, ExecutedEventArgs e)
        {
            Frame.NavigationService.GoBack();
        }

        private void CanFirstPage(object sender, CanExecuteEventArgs e)
        {
            e.Cancel = ViewModel.CurrentPage == 1;
        }

        private void CanPreviousPage(object sender, CanExecuteEventArgs e)
        {
            e.Cancel = ViewModel.CurrentPage <= 1;
        }

        private void CanNextPage(object sender, CanExecuteEventArgs e)
        {
            e.Cancel = ViewModel.CurrentPage >= ViewModel.MaxPage;
        }

        private void CanLastPage(object sender, CanExecuteEventArgs e)
        {
            e.Cancel = ViewModel.CurrentPage == ViewModel.MaxPage;
        }

        private void FirstPage(object sender, ExecutedEventArgs e)
        {
            Frame.ShowLoading();
            ViewModel.CurrentPage = 1;
            ViewModel.UpdateSource();
            Frame.HideLoading();
        }

        private void PreviousPage(object sender, ExecutedEventArgs e)
        {
            Frame.ShowLoading();
            ViewModel.CurrentPage--;
            ViewModel.UpdateSource();
            Frame.HideLoading();
        }

        private void NextPage(object sender, ExecutedEventArgs e)
        {
            Frame.ShowLoading();
            ViewModel.CurrentPage++;
            ViewModel.UpdateSource();
            Frame.HideLoading();
        }

        private void LastPage(object sender, ExecutedEventArgs e)
        {
            Frame.ShowLoading();
            ViewModel.CurrentPage = ViewModel.MaxPage;
            ViewModel.UpdateSource();
            Frame.HideLoading();
        }

        private void CanAdd(object sender, CanExecuteEventArgs e)
        {
            e.Cancel = !ViewModel.ViewBuilder.AllowedAdd;
        }

        private void Add(object sender, ExecutedEventArgs e)
        {
            if (EditorItemFactory == null)
                Frame.NavigationService.NavigateTo(new Editor<TEntity>(new DefaultEditorItemFactory { Frame = Frame }));
            else
                Frame.NavigationService.NavigateTo(new Editor<TEntity>(EditorItemFactory));
        }

        private void CanEdit(object sender, ItemCanExecuteEventArgs e)
        {
            if (e.Item == null)
            {
                e.Cancel = true;
                return;
            }
            e.Cancel = !((TEntity)e.Item).CanEdit();
        }

        private void Edit(object sender, ItemExecutedEventArgs e)
        {
            EntityEditorViewModel<TEntity> model = new EntityEditorViewModel<TEntity>(BussinessApplication.Current.ContextBuilder.GetContext<TEntity>());
            model.Item = (TEntity)e.Item;
            if (EditorItemFactory == null)
                Frame.NavigationService.NavigateTo(new Editor<TEntity>(model, new DefaultEditorItemFactory { Frame = Frame }));
            else
                Frame.NavigationService.NavigateTo(new Editor<TEntity>(model, EditorItemFactory));
        }

        private void CanRemove(object sender, ItemCanExecuteEventArgs e)
        {
            if (e.Item == null)
            {
                e.Cancel = true;
                return;
            }
            e.Cancel = !((TEntity)e.Item).CanRemove();
        }

        private void Remove(object sender, ItemExecutedEventArgs e)
        {
            BussinessApplication.Current.ContextBuilder.GetContext<TEntity>().Remove(((TEntity)e.Item).Index);
            ViewModel.Items.Remove(((TEntity)e.Item).Index);
            ViewModel.UpdateSource();
        }

        #endregion

        protected override void OnNavigatedTo(WorkPage page)
        {
            Title = Frame.MainTitle + " 列表";

            if (page is Editor<TEntity>)
            {
                Editor<TEntity> editor = (Editor<TEntity>)page;
                if (editor.DialogResult == false)
                    return;
                if (editor.ViewModel.Item.BaseIndex == default(Guid))
                {
                    editor.ViewModel.Item.BaseIndex = Guid.NewGuid();
                    BussinessApplication.Current.ContextBuilder.GetContext<TEntity>().Add(editor.ViewModel.Item);
                    ViewModel.Items.Insert(0, editor.ViewModel.Item.BaseIndex);
                    ViewModel.CurrentPage = 1;
                    ViewModel.UpdateSource();
                }
                else
                {
                    BussinessApplication.Current.ContextBuilder.GetContext<TEntity>().Edit(editor.ViewModel.Item);
                }
            }

            base.OnNavigatedTo(page);
        }

        public System.Collections.ObjectModel.ObservableCollection<AppButton> Buttons { get; private set; }

        public EditorItemFactory EditorItemFactory { get; set; }

        private void Buttons_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                foreach (AppButton button in e.OldItems)
                    AppButtonPanel.Items.Remove(button);
            if (e.NewItems != null)
                foreach (AppButton button in e.NewItems)
                    AppButtonPanel.Items.Add(button);
        }
    }
}

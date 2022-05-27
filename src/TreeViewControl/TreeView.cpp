#pragma once
#include "pch.h"
#include "TreeView.h"
#include <ppltasks.h>
using namespace Windows::UI::Xaml;
using namespace Windows::UI::Xaml::Interop;
using namespace Windows::UI::Xaml::Controls;
using namespace concurrency;

namespace TreeViewControl {

    TreeView::TreeView()
    {
        flatViewModel = ref new ViewModel;
        rootNode = ref new TreeNode();

        flatViewModel->ExpandNode(rootNode);

        CanReorderItems = false;
        AllowDrop = false;
        CanDragItems = false;

		evhan = ref new BindableVectorChangedEventHandler(flatViewModel, &ViewModel::TreeNodeVectorChanged);
		handlerCookie = rootNode->VectorChanged += evhan;
        ItemClick += ref new Windows::UI::Xaml::Controls::ItemClickEventHandler(this, &TreeView::TreeView_OnItemClick);
        DragItemsStarting += ref new Windows::UI::Xaml::Controls::DragItemsStartingEventHandler(this, &TreeView::TreeView_DragItemsStarting);
        DragItemsCompleted += ref new Windows::Foundation::TypedEventHandler<Windows::UI::Xaml::Controls::ListViewBase ^, Windows::UI::Xaml::Controls::DragItemsCompletedEventArgs ^>(this, &TreeView::TreeView_DragItemsCompleted);
        ItemsSource = flatViewModel;
    }

    void TreeView::TreeView_OnItemClick(Platform::Object^ sender, Windows::UI::Xaml::Controls::ItemClickEventArgs^ args)
    {
        TreeViewItemClickEventArgs^ treeViewArgs = ref new TreeViewItemClickEventArgs();
        treeViewArgs->ClickedItem = args->ClickedItem;

        TreeViewItemClick(this, treeViewArgs);

        if (!treeViewArgs->IsHandled)
        {
            TreeNode^ targetNode = (TreeNode^)args->ClickedItem;
            if (targetNode->IsExpanded)
            {
                flatViewModel->CollapseNode(targetNode);
            }
            else
            {
                flatViewModel->ExpandNode(targetNode);                
            }
        }
    }

    void TreeView::TreeView_DragItemsStarting(Platform::Object^ sender, Windows::UI::Xaml::Controls::DragItemsStartingEventArgs^ e)
    {
        draggedTreeViewItem = (TreeViewItem^)this->ContainerFromItem(e->Items->GetAt(0));
    }

    void TreeView::TreeView_DragItemsCompleted(Windows::UI::Xaml::Controls::ListViewBase^ sender, Windows::UI::Xaml::Controls::DragItemsCompletedEventArgs^ args)
    {
        draggedTreeViewItem = nullptr;
    }

    void TreeView::OnDrop(Windows::UI::Xaml::DragEventArgs^ e)
    {
        if (e->AcceptedOperation == Windows::ApplicationModel::DataTransfer::DataPackageOperation::Move)
        {
            Panel^ panel = this->ItemsPanelRoot;
            Windows::Foundation::Point point = e->GetPosition(panel);

            int aboveIndex = -1;
            int belowIndex = -1;
            unsigned int relativeIndex;

            IInsertionPanel^ insertionPanel = (IInsertionPanel^)panel;

            if (insertionPanel != nullptr)
            {
                insertionPanel->GetInsertionIndexes(point, &aboveIndex, &belowIndex);

                TreeNode^ aboveNode = (TreeNode^)flatViewModel->GetAt(aboveIndex);
                TreeNode^ belowNode = (TreeNode^)flatViewModel->GetAt(belowIndex);
                TreeNode^ targetNode = (TreeNode^)this->ItemFromContainer(draggedTreeViewItem);

                //Between two items
                if (aboveNode && belowNode)
                {
                    targetNode->ParentNode->IndexOf(targetNode, &relativeIndex);
                    targetNode->ParentNode->RemoveAt(relativeIndex);

                    if (belowNode->ParentNode == aboveNode)
                    {
                        aboveNode->InsertAt(0, targetNode);
                    }
                    else
                    {
                        aboveNode->ParentNode->IndexOf(aboveNode, &relativeIndex);
                        aboveNode->ParentNode->InsertAt(relativeIndex + 1, targetNode);
                    }
                }
                //Bottom of the list
                else if (aboveNode && !belowNode)
                {
                    targetNode->ParentNode->IndexOf(targetNode, &relativeIndex);
                    targetNode->ParentNode->RemoveAt(relativeIndex);

                    aboveNode->ParentNode->IndexOf(aboveNode, &relativeIndex);
                    aboveNode->ParentNode->InsertAt(relativeIndex + 1, targetNode);
                }
                //Top of the list
                else if (!aboveNode && belowNode)
                {
                    targetNode->ParentNode->IndexOf(targetNode, &relativeIndex);
                    targetNode->ParentNode->RemoveAt(relativeIndex);

                    rootNode->InsertAt(0, targetNode);
                }
            }
        }

        e->Handled = true;
        ListViewBase::OnDrop(e);
    }

    void TreeView::OnDragOver(Windows::UI::Xaml::DragEventArgs^ e)
    {
        Windows::ApplicationModel::DataTransfer::DataPackageOperation savedOperation = Windows::ApplicationModel::DataTransfer::DataPackageOperation::None;

        e->AcceptedOperation = Windows::ApplicationModel::DataTransfer::DataPackageOperation::None;

        Panel^ panel = this->ItemsPanelRoot;
        Windows::Foundation::Point point = e->GetPosition(panel);

        int aboveIndex = -1;
        int belowIndex = -1;

        IInsertionPanel^ insertionPanel = (IInsertionPanel^)panel;

        if (insertionPanel != nullptr)
        {
            insertionPanel->GetInsertionIndexes(point, &aboveIndex, &belowIndex);

            if (aboveIndex > -1)
            {
                TreeNode^ aboveNode = (TreeNode^)flatViewModel->GetAt(aboveIndex);
                TreeNode^ targetNode = (TreeNode^)this->ItemFromContainer(draggedTreeViewItem);

                TreeNode^ ancestorNode = aboveNode;
                while (ancestorNode != nullptr && ancestorNode != targetNode)
                {
                    ancestorNode = ancestorNode->ParentNode;
                }

                if (ancestorNode == nullptr)
                {
                    savedOperation = Windows::ApplicationModel::DataTransfer::DataPackageOperation::Move;
                    e->AcceptedOperation = Windows::ApplicationModel::DataTransfer::DataPackageOperation::Move;
                }
            }
            else
            {
                savedOperation = Windows::ApplicationModel::DataTransfer::DataPackageOperation::Move;
                e->AcceptedOperation = Windows::ApplicationModel::DataTransfer::DataPackageOperation::Move;
            }
        }

        ListViewBase::OnDragOver(e);
        e->AcceptedOperation = savedOperation;
    }

    void TreeView::ExpandNode(TreeNode^ targetNode)
    {
        flatViewModel->ExpandNode(targetNode);
    }

    void TreeView::CollapseNode(TreeNode^ targetNode)
    {
        flatViewModel->CollapseNode(targetNode);
    }

    void TreeView::PrepareContainerForItemOverride(DependencyObject^ element, Object^ item)
    {
        ((UIElement^)element)->AllowDrop = true;

        ListView::PrepareContainerForItemOverride(element, item);
    }

    DependencyObject^ TreeView::GetContainerForItemOverride()
    {
        TreeViewItem^ targetItem = ref new TreeViewItem();
        return (DependencyObject^)targetItem;
    }

	void TreeView::newNode() {
		rootNode->childrenVector->Clear();
		flatViewModel->Clear();

		flatViewModel->ExpandNode(rootNode);

		evhan = ref new BindableVectorChangedEventHandler(flatViewModel, &ViewModel::TreeNodeVectorChanged);
		handlerCookie = rootNode->VectorChanged += evhan;
		ItemClick += ref new Windows::UI::Xaml::Controls::ItemClickEventHandler(this, &TreeView::TreeView_OnItemClick);
		DragItemsStarting += ref new Windows::UI::Xaml::Controls::DragItemsStartingEventHandler(this, &TreeView::TreeView_DragItemsStarting);
		DragItemsCompleted += ref new Windows::Foundation::TypedEventHandler<Windows::UI::Xaml::Controls::ListViewBase ^, Windows::UI::Xaml::Controls::DragItemsCompletedEventArgs ^>(this, &TreeView::TreeView_DragItemsCompleted);
		ItemsSource = flatViewModel;
	}

	void TreeView::AddRange(Windows::UI::Xaml::Interop::IBindableIterable^ vector) {
		rootNode->VectorChanged -= handlerCookie;
		rootNode->childrenVector->VectorChanged -= rootNode->childVectorChangedEventToken;
		IBindableIterator^ iter = vector->First();
		int i = 0;
		while (iter->HasCurrent) {
			rootNode->Append(iter->Current);
			flatViewModel->InsertAt(i, iter->Current);
			iter->MoveNext();
			i++;
		}
		//handlerCookie = rootNode->VectorChanged += evhan; //Is this unnecessary?
		//rootNode->childVectorChangedEventToken = rootNode->childrenVector->VectorChanged += rootNode->evhan; //Is this also unnecessary?
	}

	void TreeView::Clear() {
		rootNode->VectorChanged -= handlerCookie;
		rootNode->childrenVector->VectorChanged -= rootNode->childVectorChangedEventToken;
		rootNode->childrenVector->Clear();
		flatViewModel->Clear();
		handlerCookie = rootNode->VectorChanged += evhan;
		rootNode->childVectorChangedEventToken = rootNode->childrenVector->VectorChanged += rootNode->evhan;

	}


	void TreeView::buildNodeInBackground(Windows::UI::Xaml::Interop::IBindableIterable^ vector) {

		Windows::Foundation::IAsyncAction^ Op1 = create_async([vector] {
			ViewModel^ new_flatViewModel = ref new ViewModel();
			TreeNode^ new_rootNode = ref new TreeNode();
			//new_flatViewModel->ExpandNode(new_rootNode);
			IBindableIterator^ iter = vector->First();
			int i = 0;
			while (iter->HasCurrent) {
				new_rootNode->Append(iter->Current);
				new_flatViewModel->InsertAt(i, iter->Current);
				iter->MoveNext();
				i++;
			}
		});
		create_task(Op1).then([] () {

		});
	}

}
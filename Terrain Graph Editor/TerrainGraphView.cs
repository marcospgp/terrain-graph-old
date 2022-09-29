#if UNITY_EDITOR

using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.UIElements;

namespace MarcosPereira.Terrain {
    // This class was created in order to hide things from the contextual menu
    // (the menu that appears when one right-clicks on the canvas).
    public class TerrainGraphView : GraphView {
        public TerrainGraphView(
            GraphViewEditorWindow window,
            CommandDispatcher commandDispatcher,
            string graphViewName
        ) : base(window, commandDispatcher, graphViewName) {
        }

        // Simply copied the original method and removed the parts we don't want.
        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt) {
#pragma warning disable
            if (CommandDispatcher == null)
                return;

            if (evt.menu.MenuItems().Count > 0)
                evt.menu.AppendSeparator();

            evt.menu.AppendAction("Create Node", menuAction =>
            {
                Vector2 mousePosition = menuAction?.eventInfo?.mousePosition ?? Event.current.mousePosition;
                DisplaySmartSearch(mousePosition);
            });

            // Had to disable placemats because they cause "assertion failed"
            // exceptions to be thrown by the UI package (UI toolkit, I believe).

            // evt.menu.AppendAction("Create Placemat", menuAction =>
            // {
            //     Vector2 mousePosition = menuAction?.eventInfo?.mousePosition ?? Event.current.mousePosition;
            //     Vector2 graphPosition = ContentViewContainer.WorldToLocal(mousePosition);

            //     CommandDispatcher.Dispatch(new CreatePlacematCommand(new Rect(graphPosition.x, graphPosition.y, 200, 200)));
            // });

            var selection = GetSelection().ToList();
            if (selection.Any())
            {
                var nodesAndNotes = selection.
                    Where(e => e is INodeModel || e is IStickyNoteModel).
                    Select(m => m.GetUI<GraphElement>(this)).ToList();

                // evt.menu.AppendAction("Create Placemat Under Selection", _ =>
                // {
                //     Rect bounds = new Rect();
                //     if (Placemat.ComputeElementBounds(ref bounds, nodesAndNotes))
                //     {
                //         CommandDispatcher.Dispatch(new CreatePlacematCommand(bounds));
                //     }
                // }, nodesAndNotes.Count == 0 ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);

                /* Actions on selection */

                // Had to comment out alignment stuff because it relies on
                // members with internal only accessibility.

                // evt.menu.AppendSeparator();

                // var itemName = ShortcutHelper.CreateShortcutMenuItemEntry("Align Elements/Align Items", GraphModel.Stencil.ToolName, ShortcutAlignNodesEvent.id);
                // evt.menu.AppendAction(itemName, _ =>
                // {
                //     CommandDispatcher.Dispatch(new AlignNodesCommand(this, false, GetSelection()));
                // });

                // itemName = ShortcutHelper.CreateShortcutMenuItemEntry("Align Elements/Align Hierarchy", GraphModel.Stencil.ToolName, ShortcutAlignNodeHierarchiesEvent.id);
                // evt.menu.AppendAction(itemName, _ =>
                // {
                //     CommandDispatcher.Dispatch(new AlignNodesCommand(this, true, GetSelection()));
                // });

                // var selectionUI = selection.Select(m => m.GetUI<GraphElement>(this));
                // if (selectionUI.Count(elem => elem != null && !(elem.Model is IEdgeModel) && elem.visible) > 1)
                // {


                    // evt.menu.AppendAction("Align Elements/Top",
                    //     _ => m_AutoAlignmentHelper.SendAlignCommand(AutoAlignmentHelper.AlignmentReference.Top));

                    // evt.menu.AppendAction("Align Elements/Bottom",
                    //     _ => m_AutoAlignmentHelper.SendAlignCommand(AutoAlignmentHelper.AlignmentReference.Bottom));

                    // evt.menu.AppendAction("Align Elements/Left",
                    //     _ => m_AutoAlignmentHelper.SendAlignCommand(AutoAlignmentHelper.AlignmentReference.Left));

                    // evt.menu.AppendAction("Align Elements/Right",
                    //     _ => m_AutoAlignmentHelper.SendAlignCommand(AutoAlignmentHelper.AlignmentReference.Right));

                    // evt.menu.AppendAction("Align Elements/Horizontal Center",
                    //     _ => m_AutoAlignmentHelper.SendAlignCommand(AutoAlignmentHelper.AlignmentReference.HorizontalCenter));

                    // evt.menu.AppendAction("Align Elements/Vertical Center",
                    //     _ => m_AutoAlignmentHelper.SendAlignCommand(AutoAlignmentHelper.AlignmentReference.VerticalCenter));

                    // evt.menu.AppendAction("Space Elements/Horizontal",
                    //     _ => m_AutoSpacingHelper.SendSpacingCommand(PortOrientation.Horizontal));

                    // evt.menu.AppendAction("Space Elements/Vertical",
                    //     _ => m_AutoSpacingHelper.SendSpacingCommand(PortOrientation.Vertical));
                // }

                var nodes = selection.OfType<INodeModel>().ToList();
                if (nodes.Count > 0)
                {
                    var connectedNodes = nodes
                        .Where(m => m.GetConnectedEdges().Any())
                        .ToList();

                    evt.menu.AppendAction("Disconnect Nodes", _ =>
                    {
                        CommandDispatcher.Dispatch(new DisconnectNodeCommand(connectedNodes));
                    }, connectedNodes.Count == 0 ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);

                    var ioConnectedNodes = connectedNodes
                        .OfType<IInputOutputPortsNodeModel>()
                        .Where(x => x.InputsByDisplayOrder.Any(y => y.IsConnected()) &&
                            x.OutputsByDisplayOrder.Any(y => y.IsConnected())).ToList();

                    // Bypassing nodes does not save that much time and can lead to issues.
                    // For example, nodes deleted by bypassing would still have Repaint() called
                    // and would throw null reference exceptions on every Update().
                    //
                    // evt.menu.AppendAction("Bypass Nodes", _ =>
                    // {
                    //     CommandDispatcher.Dispatch(new BypassNodesCommand(ioConnectedNodes, nodes));
                    // }, ioConnectedNodes.Count == 0 ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);

                    // Why do we need to disable nodes? Not even sure how to handle that in code.
                    //
                    // var willDisable = nodes.Any(n => n.State == ModelState.Enabled);
                    // evt.menu.AppendAction(willDisable ? "Disable Nodes" : "Enable Nodes", _ =>
                    // {
                    //     CommandDispatcher.Dispatch(new ChangeNodeStateCommand(willDisable ? ModelState.Disabled : ModelState.Enabled, nodes));
                    // });
                }

                if (selection.Count == 2)
                {
                    // PF: FIXME check conditions correctly for this actions (exclude single port nodes, check if already connected).
                    if (selection.FirstOrDefault(x => x is IEdgeModel) is IEdgeModel edgeModel &&
                        selection.FirstOrDefault(x => x is IInputOutputPortsNodeModel) is IInputOutputPortsNodeModel nodeModel)
                    {
                        evt.menu.AppendAction("Insert Node on Edge", _ => CommandDispatcher.Dispatch(new SplitEdgeAndInsertExistingNodeCommand(edgeModel, nodeModel)),
                            eventBase => DropdownMenuAction.Status.Normal);
                    }
                }

                /*
                var variableNodes = nodes.OfType<IVariableNodeModel>().ToList();
                var constants = nodes.OfType<IConstantNodeModel>().ToList();
                if (variableNodes.Count > 0)
                {
                    // TODO JOCE We might want to bring the concept of Get/Set variable from VS down to GTF
                    itemName = ShortcutHelper.CreateShortcutMenuItemEntry("Variable/Convert", GraphModel.Stencil.ToolName, ShortcutConvertConstantAndVariableEvent.id);
                    evt.menu.AppendAction(itemName,
                        _ => CommandDispatcher.Dispatch(new ConvertConstantNodesAndVariableNodesCommand(null, variableNodes)),
                        variableNodes.Any(v => v.OutputsByDisplayOrder.Any(o => o.PortType == PortType.Data)) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

                    evt.menu.AppendAction("Variable/Itemize",
                        _ => CommandDispatcher.Dispatch(new ItemizeNodeCommand(variableNodes.OfType<ISingleOutputPortNodeModel>().ToList())),
                        variableNodes.Any(v => v.OutputsByDisplayOrder.Any(o => o.PortType == PortType.Data && o.GetConnectedPorts().Count() > 1)) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                }

                if (constants.Count > 0)
                {
                    itemName = ShortcutHelper.CreateShortcutMenuItemEntry("Constant/Convert", GraphModel.Stencil.ToolName, ShortcutConvertConstantAndVariableEvent.id);
                    evt.menu.AppendAction(itemName,
                        _ => CommandDispatcher.Dispatch(new ConvertConstantNodesAndVariableNodesCommand(constants, null)), x => DropdownMenuAction.Status.Normal);

                    evt.menu.AppendAction("Constant/Itemize",
                        _ => CommandDispatcher.Dispatch(new ItemizeNodeCommand(constants.OfType<ISingleOutputPortNodeModel>().ToList())),
                        constants.Any(v => v.OutputsByDisplayOrder.Any(o => o.PortType == PortType.Data && o.GetConnectedPorts().Count() > 1)) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

                    evt.menu.AppendAction("Constant/Lock",
                        _ => CommandDispatcher.Dispatch(new LockConstantNodeCommand(constants, true)),
                        x =>
                            constants.Any(e => !e.IsLocked) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled
                    );

                    evt.menu.AppendAction("Constant/Unlock",
                        _ => CommandDispatcher.Dispatch(new LockConstantNodeCommand(constants, false)),
                        x =>
                            constants.Any(e => e.IsLocked) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled
                    );
                }

                var portals = nodes.OfType<IEdgePortalModel>().ToList();
                if (portals.Count > 0)
                {
                    var canCreate = portals.Where(p => p.CanCreateOppositePortal()).ToList();
                    evt.menu.AppendAction("Create Opposite Portal",
                        _ =>
                        {
                            CommandDispatcher.Dispatch(new CreateOppositePortalCommand(canCreate));
                        }, canCreate.Count > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                }
                */

                // GraphViewStaticBridge is internal only, have to comment this out.
                /*
                var colorables = selection.Where(s => s.IsColorable()).ToList();
                if (colorables.Any())
                {
                    evt.menu.AppendAction("Color/Change...", _ =>
                    {
                        void ChangeNodesColor(Color pickedColor)
                        {
                            CommandDispatcher.Dispatch(new ChangeElementColorCommand(pickedColor, colorables));
                        }

                        var defaultColor = new Color(0.5f, 0.5f, 0.5f);
                        if (colorables.Count == 1)
                        {
                            defaultColor = colorables[0].Color;
                        }

                        GraphViewStaticBridge.ShowColorPicker(ChangeNodesColor, defaultColor, true);
                    });

                    evt.menu.AppendAction("Color/Reset", _ =>
                    {
                        CommandDispatcher.Dispatch(new ResetElementColorCommand(colorables));
                    });
                }
                else
                {
                    evt.menu.AppendAction("Color", _ => { }, eventBase => DropdownMenuAction.Status.Disabled);
                }
                */

                /*
                var edges = selection.OfType<IEdgeModel>().ToList();
                if (edges.Count > 0)
                {
                    evt.menu.AppendSeparator();

                    var edgeData = edges.Select(
                        edgeModel =>
                        {
                            var outputPort = edgeModel.FromPort.GetUI<Port>(this);
                            var inputPort = edgeModel.ToPort.GetUI<Port>(this);
                            var outputNode = edgeModel.FromPort.NodeModel.GetUI<Node>(this);
                            var inputNode = edgeModel.ToPort.NodeModel.GetUI<Node>(this);

                            if (outputNode == null || inputNode == null || outputPort == null || inputPort == null)
                                return (null, Vector2.zero, Vector2.zero);

                            return (edgeModel,
                                outputPort.ChangeCoordinatesTo(outputNode.parent, outputPort.layout.center),
                                inputPort.ChangeCoordinatesTo(inputNode.parent, inputPort.layout.center));
                        }
                        ).Where(tuple => tuple.Item1 != null).ToList();

                    evt.menu.AppendAction("Create Portals", _ =>
                    {
                        CommandDispatcher.Dispatch(new ConvertEdgesToPortalsCommand(edgeData));
                    });
                }
                */

                var stickyNotes = selection.OfType<IStickyNoteModel>().ToList();

                if (stickyNotes.Count > 0)
                {
                    evt.menu.AppendSeparator();

                    DropdownMenuAction.Status GetThemeStatus(DropdownMenuAction a)
                    {
                        if (stickyNotes.Any(noteModel => noteModel.Theme != stickyNotes.First().Theme))
                        {
                            // Values are not all the same.
                            return DropdownMenuAction.Status.Normal;
                        }

                        return stickyNotes.First().Theme == (a.userData as string) ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
                    }

                    DropdownMenuAction.Status GetSizeStatus(DropdownMenuAction a)
                    {
                        if (stickyNotes.Any(noteModel => noteModel.TextSize != stickyNotes.First().TextSize))
                        {
                            // Values are not all the same.
                            return DropdownMenuAction.Status.Normal;
                        }

                        return stickyNotes.First().TextSize == (a.userData as string) ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
                    }

                    foreach (var value in StickyNote.GetThemes())
                    {
                        evt.menu.AppendAction("Sticky Note Theme/" + value,
                            menuAction => CommandDispatcher.Dispatch(new UpdateStickyNoteThemeCommand(menuAction.userData as string, stickyNotes)),
                            GetThemeStatus, value);
                    }

                    foreach (var value in StickyNote.GetSizes())
                    {
                        evt.menu.AppendAction("Sticky Note Text Size/" + value,
                            menuAction => CommandDispatcher.Dispatch(new UpdateStickyNoteTextSizeCommand(menuAction.userData as string, stickyNotes)),
                            GetSizeStatus, value);
                    }
                }
            }

            evt.menu.AppendSeparator();

            evt.menu.AppendAction("Cut", (a) => { CutSelectionCallback(); },
                CanCutSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction("Copy", (a) => { CopySelectionCallback(); },
                CanCopySelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction("Paste", (a) => { PasteCallback(); },
                CanPaste ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendSeparator();

            evt.menu.AppendAction("Duplicate", (a) => { DuplicateSelectionCallback(); },
                CanDuplicateSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction("Delete", _ =>
            {
                CommandDispatcher.Dispatch(new DeleteElementsCommand(selection.ToList()));
            }, CanDeleteSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            /*
            if (Unsupported.IsDeveloperBuild())
            {
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Internal/Refresh All UI", _ =>
                {
                    using (var updater = CommandDispatcher.State.GraphViewState.UpdateScope)
                    {
                        updater.ForceCompleteUpdate();
                    }
                });

                if (selection.Any())
                {
                    evt.menu.AppendAction("Internal/Refresh Selected Element(s)",
                        _ =>
                        {
                            using (var graphUpdater = CommandDispatcher.State.GraphViewState.UpdateScope)
                            {
                                graphUpdater.MarkChanged(selection);
                            }
                        });
                }
            }
            */
#pragma warning restore
        }
    }
}

#endif

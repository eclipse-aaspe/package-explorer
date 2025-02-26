/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/
using AdminShellNS;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace Extensions
{
    public static class ExtendOperation
    {
        #region AasxPackageExplorer

        public static object AddChild(this IOperation operation, ISubmodelElement childSubmodelElement, EnumerationPlacmentBase placement = null)
        {
            // not enough information to select list of children?
            var pl = placement as EnumerationPlacmentOperationVariable;
            if (childSubmodelElement == null || pl == null)
                return null;

            // ok, use information
            var ov = new OperationVariable(childSubmodelElement);

            if (childSubmodelElement != null)
                childSubmodelElement.Parent = operation;

            if (pl.Direction == OperationVariableDirection.In)
            {
                operation.InputVariables ??= new List<IOperationVariable>();
                operation.InputVariables.Add(ov);
            }

            if (pl.Direction == OperationVariableDirection.Out)
            {
                operation.OutputVariables ??= new List<IOperationVariable>();
                operation.OutputVariables.Add(ov);
            }

            if (pl.Direction == OperationVariableDirection.InOut)
            {
                operation.InoutputVariables ??= new List<IOperationVariable>();
                operation.InoutputVariables.Add(ov);
            }

            return ov;
        }

        public static EnumerationPlacmentBase GetChildrenPlacement(this IOperation operation, ISubmodelElement child)
        {
            // trivial
            if (child == null)
                return null;

            // search
            OperationVariableDirection? dir = null;
            IOperationVariable opvar = null;
            if (operation.InputVariables != null)
                foreach (var ov in operation.InputVariables)
                    if (ov?.Value == child)
                    {
                        dir = OperationVariableDirection.In;
                        opvar = ov;
                    }

            if (operation.OutputVariables != null)
                foreach (var ov in operation.OutputVariables)
                    if (ov?.Value == child)
                    {
                        dir = OperationVariableDirection.Out;
                        opvar = ov;
                    }

            if (operation.InoutputVariables != null)
                foreach (var ov in operation.InoutputVariables)
                    if (ov?.Value == child)
                    {
                        dir = OperationVariableDirection.InOut;
                        opvar = ov;
                    }

            // found
            if (!dir.HasValue)
                return null;
            return new EnumerationPlacmentOperationVariable()
            {
                Direction = dir.Value,
                OperationVariable = opvar as OperationVariable
            };
        }

        public static List<IOperationVariable> GetVars(this IOperation op, OperationVariableDirection dir)
        {
            if (dir == OperationVariableDirection.In)
                return op.InputVariables;
            if (dir == OperationVariableDirection.Out)
                return op.OutputVariables;
            return op.InoutputVariables;
        }

        public static List<IOperationVariable> SetVars(
            this IOperation op, OperationVariableDirection dir, List<IOperationVariable> value)
        {
            if (dir == OperationVariableDirection.In)
            {
                op.InputVariables = value;
                return op.InputVariables;
            }
            if (dir == OperationVariableDirection.Out)
            {
                op.OutputVariables = value;
                return op.OutputVariables;
            }

            op.InoutputVariables = value;
            return op.InoutputVariables;
        }

        public static void Add(this Operation operation, ISubmodelElement submodelElement,
            OperationVariableDirection direction = OperationVariableDirection.In)
        {
            var ovl = GetVars(operation, direction);
            if (ovl == null)
            {
                ovl = new List<IOperationVariable>();
                SetVars(operation, direction, ovl);
            }
            ovl.Add(new OperationVariable(submodelElement));
        }

        public static void Remove(this Operation operation, ISubmodelElement submodelElement)
        {
            foreach (var ovd in AdminShellUtil.GetEnumValues<OperationVariableDirection>())
            {
                var ovl = GetVars(operation, ovd);
                if (ovl == null)
                    continue;
                foreach (var ov in ovl)
                    if (submodelElement != null
                        && ov.Value == submodelElement)
                    {
                        ovl.Remove(ov);
                        break;
                    }
            }
        }

        public static int Replace(
            this Operation operation,
            ISubmodelElement oldElem, ISubmodelElement newElem)
        {
            foreach (var ovd in AdminShellUtil.GetEnumValues<OperationVariableDirection>())
            {
                var ovl = GetVars(operation, ovd);
                foreach (var ov in ovl)
                    if (oldElem != null
                        && ov.Value == oldElem)
                    {
                        ov.Value = newElem;
                        return 1;
                    }
            }
            return -1;
        }

        #endregion

        public static IOperation UpdateFrom(
            this IOperation elem, ISubmodelElement source)
        {
            if (source == null)
                return elem;

            ((ISubmodelElement)elem).UpdateFrom(source);

            if (source is SubmodelElementCollection srcColl)
            {
                if (srcColl.Value != null)
                {
                    List<OperationVariable> operationVariables = srcColl.Value.Copy().Select(
                        (isme) => new OperationVariable(isme)).ToList();
                    elem.InputVariables = operationVariables.ConvertAll(op => (IOperationVariable)op);
                }

            }

            if (source is SubmodelElementCollection srcList)
            {
                if (srcList.Value != null)
                {
                    List<OperationVariable> operationVariables = srcList.Value.Copy().Select(
                        (isme) => new OperationVariable(isme)).ToList();
                    elem.InputVariables = operationVariables.ConvertAll(op => (IOperationVariable)op);
                }
            }

            return elem;
        }
    }
}

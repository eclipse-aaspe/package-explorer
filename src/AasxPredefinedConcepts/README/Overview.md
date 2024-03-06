# Overview on organization of AASPE predefined concepts

## General

This projects hold a wealth of classes defining concepts, in particular
semanticIds and class structures. These concepts could be used on 
*source code level* to avoid having semanticIds as sttring constants.

Nearly everything here was produced by the export function:
AASPE / File / Export / Export predefined concepts.

Multiple low level SDK function are designed for using these symbols, e.g.

```
foreach (var srcLst in this.theSubmodel.SubmodelElements
    .FindAllSemanticIdAs<Aas.SubmodelElementCollection>(
        AasxPredefinedConcepts.DefinitionsMTP.ModuleTypePackage.CD_SourceList?.GetReference(), 
        MatchMode.Relaxed))
{
    ...
}
```

## Programmers note

As many (!) classes and attributes are declared, these classes seem contribute
significantly to the loading time of AASPE in general. Basically, these classes
seem to be loaded on demand by the class loader.

However, when using "Add known .." button in References dialogue, the user 
has to wait some 8sec for the first results, as *ALL* classes are firstly 
instantiated for this scan.

## Namespaces

Currently, the folder names do not influence the namespaces of the classes, because
handling would even get longer.

## BaseTopUtil

These classes are either base classes, utility functions or the root definitions,
where all others are linked to.

Note: New definition classes shall be linked within `DefinitionsPool.cs`.

## Concept model

(Deprecated) approach to provide a switcher between different sets of concepts,
here for some ZVEI models.

## Convert

Classes used to allow (automatic) converting between different versions of 
Submodels. Useful to define, when a large user basis is known to use an old
version of a Submodel.

## Definitions and Resources

Classes providing `CD_xxx` references having constant semanticId in it.

In newer versions of these files, a reflection approach stores the full
ConceptDescriptions (CDs) in the Resource JSON files and the source code
links against it.

This is the *preferred approach* to realize predefined data for the AASPE,
as many functions ("Add known ..", "New Submodel ..") can use these
data automatically.

In order the be properly linked, the classes need to be listed in
`DefinitionsPool.cs`.

Note: It is recommended, to *NOT* include all symbols by 
`using AasxPredefinedConcepts`, but to refer to specific classes.

Note: For the sake of shorter source code symbols, the classes in the 
`DefinitionsXXX` are cut to `XXX` because preferable preceded by 
`AasxPredefinedConcepts.`.

Note: some of the older definition classes rely on an instatiation
on-the-fly, while the new ones provide a singleton `Static`. The
latter is the recommened practice.

## Mapping

These classes support an automatic mapping of Submodel structures
to C# class hierarchies. These classes are 1:1 mapping of the
SMT structure and contain C# attributes with explicit semanticIds 
to allow functionality of `PredefinedConceptsClassMapper.cs`.

## Qualifers

These classes pre-define names, semanticIds and functions for
handling `Qualifiers`.
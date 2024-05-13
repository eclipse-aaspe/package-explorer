Current branch name: MIHO/Post_XMAS2023_Bugfixes

W.r.t. https://github.com/eclipse-aaspe/aaspe/issues/193

* [delete] Contact info plugin had false "create SM" offer
* [added] Docu plugin offer to "Documentation (V1.2)"
* "{0:00}" came from an dead innovation in Form Utils,
  changed back to "{00}" (thus, not version dependent)
* Note: I am a little concerned about the "{00}" notation (although specified and documented), as this could stand in conflict with strict frameworks not allowing "{}" in idShort. 
* Idea: allow also "__00__" ??
* Update: I've changed the code, so that "__00__" can be used in the respective .add.json-files for PresetIdShort; however, I did not implement these changes yet
* [changed] There was still in problem in translating the "{00}", so I changed the behaviour in FormInstance.cs:1203
* Now the test case is performing as intended
* Note: this forms functionality is really demanding (lots of recursions etc.), therefore it is hard to provide best quality of outcome
* What is open is the question to (overall) change notation IdShort "{00}" to "__00__"

W.r.t. https://github.com/eclipse-aaspe/aaspe/issues/35

* already done, tested

W.r.t. https://github.com/eclipse-aaspe/aaspe/issues/170

* for the normal key/value pairs edited in AASPE, I've added a tiny minimum width to the respective column in the (invisible) grid for such attributes
* thus, a tiny fraction of the field remains on screen, to indicate presence in any case
* dynamic wrapping of buttons would cause major refactoring (needs to work for WPF and HTML)
* I could add all buttons to a hamburger menu, but this doubles the clicks, therefore a abstain from this idea in general

W.r.t. https://github.com/eclipse-aaspe/aaspe/issues/33

* [changed] .aad.json-options file for ContactInformation (manually)
* export code: AasFormUtils.cs:ExportAsGenericFormsOptions()
* tested

W.r.t. https://github.com/eclipse-aaspe/aaspe/issues/30

* The respective functionality is very, very old (the original use case is unknown)
* The alternative approach is to copy/ paste all kind of JSON element data via the normal windows (text) clipboard
* [tested] copy&paste of Submodel JSON data works (surrounding square brackets for a JSON list might be required)
* I've tried to rework the old function as intended by the issuer
* [tested] Read Submodel as JSON with AAS selected or Submodel seleted (to be replaced)

W.r.t. https://github.com/eclipse-aaspe/aaspe/issues/23

* The button "Add entity .." was copy/paste artifact from anoher plugin. It was useless.
* Clicking the button lead to a un-intended and misbehavin partial update of the plugin panel.
* [deleted] the button.

W.r.t. https://github.com/eclipse-aaspe/aaspe/issues/10

* see also: https://github.com/eclipse-aaspe/aaspe/issues/193
* using "Workspace / Create / New Submodel from plugin .. " now provides a V1.2 option.

W.r.t. https://github.com/eclipse-aaspe/aaspe/issues/167

Waiting for Kazeems branch to be integrated.

W.r.t. https://github.com/eclipse-aaspe/aaspe/issues/152

* Should be fixed
* Waiting for feedback

W.r.t. https://github.com/eclipse-aaspe/aaspe/issues/145

* Waiting for feedback

W.r.t. https://github.com/eclipse-aaspe/aaspe/issues/143

* old code
* asking issuer if to delete menu item

W.r.t https://github.com/eclipse-aaspe/aaspe/issues/138

* [done] was already fixed

W.r.t. https://github.com/eclipse-aaspe/aaspe/issues/112

* [fix] menu command to call funtion in plug-in
* [fix] after importing, call re-display to show imported data
* [fix] found real bug blocking the functionality itself
* [enhance] added better debug messages, which could help understanding import failures
* [fix] imported semanticIds are created as "ExternalReference / GlobalReference" instead of "ModelReference / ConceptDescription".

W.r.t. https://github.com/eclipse-aaspe/aaspe/issues/196

* It seems to be pretty severe bug. According to the latest spec, it is not only about case-mismatch of "IEC" vs "Iec", but also the scheme changed from scheme "http://" to "https://" (Spec 01003-a-3-0 from March 2023).
* I would assume, that the Package Explorer implemented the (pre-liminary) URI and the spec changed this during the process without noticing.
* As a result, I assume that a large number of wrong data specs are now in the wild.
* For the current branch (MIHO/Post_XMAS2023_Bugfixes) I've now changed this. The AASPE uses the correct URI as a preset and takes both variants ("http://" and "https://") with case-insensitive matching.

W.r.t. https://github.com/eclipse-aaspe/aaspe/issues/174

* [quick fix] depending of length of product classification class id text, display the text larger (1.4x) or not (1.0x)
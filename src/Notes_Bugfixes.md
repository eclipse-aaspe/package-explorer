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

W.r.t. https://github.com/eclipse-aaspe/aaspe/issues/170

* for the normal key/value pairs edited in AASPE, I've added a tiny minimum width to the respective column in the (invisible) grid for such attributes
* thus, a tiny fraction of the field remains on screen, to indicate presence in any case
* dynamic wrapping of buttons would cause major refactoring (needs to work for WPF and HTML)
* I could add all buttons to a hamburger menu, but this doubles the clicks, therefore a abstain from this idea in general
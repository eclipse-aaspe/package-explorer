Current branch name: MIHO/Post_XMAS2023_Bugfixes

W.r.t. https://github.com/eclipse-aaspe/aaspe/issues/193

* [delete] Contact info plugin had false "create SM" offer
* [added] Docu plugin offer to "Documentation (V1.2)"
* "{0:00}" came from an dead innovation in Form Utils,
  changed back to "{00}" (thus, not version dependent)
* Note: I am a little concerned about the "{00}" notation (although specified and documented), as this could stand in conflict with strict frameworks not allowing "{}" in idShort. 
* Idea: allow also "__00__" ??
* Update: I've changed the code, so that "__00__" can be used in the respective .add.json-files for PresetIdShort; however, I did not implement these changes yet
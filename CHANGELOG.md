# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

[1.0.3] - 2023-2-20

### Added
 - New folder dialog when using the path editor

### Fixed
 - Unable to save for the first time
 - Multiple rules creates when pressing New rule Button
 - My files UI not creating a flexbox Correctly


[1.0.2] - 2023-2-19

### Fixed
 - not being able to create a new rule

[1.0.1] - 2023-2-19

### Added
 - Missing LICENSE.meta file
 - description package.json
 
### Changed
 - project file window title has been renamed to project file manager
 - project file manager window moved to Window/Zjustan/Project
 - author of package.json is now correct
 
### Fixed
 - Eding Applied path Textfield not changing path editor
 
[1.0.0] - 2023-2-19

### Added
 - Project file window. Found under Window/ZjustOps/Project files.
 - settings to the project files manager. It will show a list of rules and has buttons to create, save and reload Settings.
 - Settings Will Be saved under the ProjectSettings/ProjectFileRules.json.
 - A rule has a option for a name that will be visible when a rule is broken.
 - A rule has a options for the path. You can use a path editor or use the textbox.
 - A rule has options for allowed extensions. Under the textbox are suggestions based on the directory files.
 - A rule has Deny extensions toggle. It will invert the rules of the allowed extensions.
 - A rule has Apply to sub folders toggle. It will Allow this rule to work on sub folders


 

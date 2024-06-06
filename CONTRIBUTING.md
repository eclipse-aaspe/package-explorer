# Contributing

Welcome to the Eclipse AASX Package Explorer repository. 

Whether you're new to the project or joining from the previous repository at admin-shell-io, please see the [Joining Development](#joining-development) section below to get started in the new repository.

The [Development guildelines](#development-guidelines) section includes general guidelines as well as additional quirks specific to Eclipse Foundation projects, so it is definitely worth a read as well.

## Joining Development

### Eclipse Foundation incubated project

AASX Package Explorer is an Eclipse Foundation incubated project and therefore needs to adhere to Eclipse Foundation rules.
This introduces a slight overhead for every contributor, at least at the very beginning, but it's worth it!

Follow these steps to become a Contributor in Eclipse AASX Package Explorer (and any other project under the Eclipse Foundation umbrella):

1. Create an Eclipse Foundation Account at https://accounts.eclipse.org/ 

1. Log in to your Eclipse account and digitally sign the [Eclipse Contributor Agreement](https://www.eclipse.org/projects/handbook/#contributing-eca)

1. Make sure to add your github name (`Edit my account` > `Social Media Links` > `GitHub Username`)

1. Clone or fork the repository and create an initial commit
    - if you have preexisting work that you'd like to include, see [Migrating Development](#migrating-development)
    - ensure that the author credentials on the commit record match the email address and/or GitHub username associated with the Eclipse Foundation Account

1. Create a pull request and work with the project team to merge the contribution
    - your request will need to be reviewed and approved by an Eclipse Contributor (GitHub Maintainer role) 

1. Happily develop ever after!
    - or contact a Project Lead or a Committer to be nominated to a Committer yourself (and receive more repository priviledges, such as the ability to merge)

For more details see ["Contributing" in Eclipse Foundation Handbook](https://www.eclipse.org/projects/handbook/#contributing-contributors).
   
### Migrating development

If you are moving over from admin-shell-io, you can simply clone the repository anew and work on a clean slate. 

However, if you have local changes that you want to take over, you will need to update the remotes with the new repository url.

Here is how to  
switch from `https://github.com/admin-shell-io/aasx-package-explorer`  
to `https://github.com/eclipse-aaspe/aaspe`  
from the command line:

1. First, we suggest to `fetch`/`pull` and `commit` all local changes.
 
2. We can check the current remote anytime with `git remote`:
 
    ```
    D:\aasx-package-explorer> git remote -vvv
    origin  https://github.com/admin-shell-io/aasx-package-explorer (fetch)
    origin  https://github.com/admin-shell-io/aasx-package-explorer (push)
    ```
 
2. Remove the old remote called "origin" with `git remote rm <remote-name>`
 
    ```
    D:\aasx-package-explorer> git remote rm origin
    ```
 
3. Add the new repository url as a remote, also called "origin", with `git remote add <new-remote-name> <new-remote-url>`
 
    ```
    D:\aasx-package-explorer> git remote add origin https://github.com/eclipse-aaspe/aaspe
    D:\aasx-package-explorer> git remote -vvv
    origin  https://github.com/eclipse-aaspe/aaspe (fetch)
    origin  https://github.com/eclipse-aaspe/aaspe (push)
    ```
 
4. The git links have been successfully updated. Now you can push a local branch with your work to the new repository using `git push -u <remote-name> <new-remote-branch-name>`
 
    ```
    D:\aasx-package-explorer> git push -u origin mf/branch
    ```
 
> [!note]
> Please note, it would also work to leave the "origin" untouched and just create a second remote, for example called "aaspe".
> In this case, you need to specify which remote you want to pull from/push to.
> It is easier to stick to the usual default remote name "origin".

## Development guidelines

tbd:

- disabled projects
- dependencies
- pipelining/admin/configuration
- releases

> [!tip]
> Additional information can be found at the [devdocs](https://admin-shell-io.github.io/aasx-package-explorer/devdoc/) (work-in-progress)

### How to contribute

Mechanics of how to actually contribute (e.g., merge/pull requests) are described in [AASX Project Explorer Devdoc](https://admin-shell-io.github.io/aasx-package-explorer/devdoc/getting-started/intro.html).

To help you familiarize with the concept of Asset Administration Shell, we provide screencasts (both in English and German) at: https://admin-shell-io.com/screencasts/.

For further information about the Asset Administration Shell, see the publication Details of the Asset Administration Shell by Plattform Industrie 4.0.

We provide a couple of sample admin shells (packaged as .aasx) for you to test and play with the software at: http://www.admin-shell-io.com/samples/.

For a complete list of all contributing individuals and companies, please visit our [CONTRIBUTORS](CONTRIBUTORS.md) page.


### Terms of Use

This Eclipse Foundation open project is governed by the Eclipse Foundation
Development Process and all its source code as well as the released binaries are subject to Eclipse Foundation’s Terms of Use, available at https://www.eclipse.org/legal/termsofuse.php.

### Communication

Communication between contributors takes place predominantly through project issues and repository mailing list: TBD

### Repositories

Source code is publically available in the https://github.com/eclipse-aaspe/aaspe repository.

### ECA

Contributors are required to electronically sign the Eclipse Contributor Agreement (https://www.eclipse.org/legal/ECA.php) to contribute to Eclipse AASX Package Explorer™.

Commits that are provided by non-committers must contain a Signed-off-by field in
the footer indicating that the author is aware of the terms by which the
contribution has been provided to the project. The non-committer must
additionally have an Eclipse Foundation account and must have a signed Eclipse
Contributor Agreement (ECA) on file.


## Appendix: Eclipse sources - useful links

##### Development process

https://www.eclipse.org/projects/dev_process/ 

Overview/flowchart of the whole process  
https://www.eclipse.org/projects/dev_process/#6_Development_Process 

##### Project handbook

https://www.eclipse.org/projects/handbook/ 

Starting an OS project with Eclipse Foundation  
https://www.eclipse.org/projects/handbook/#starting   

User roles (User/Committer/Project Lead/Project Management Committee)  
https://www.eclipse.org/projects/handbook/#roles  

Intellectual property  
https://www.eclipse.org/projects/handbook/#ip   

Releases   
https://www.eclipse.org/projects/handbook/#release   

##### Development resources

https://wiki.eclipse.org/Development_Resources 

Getting started  
https://wiki.eclipse.org/Development_Resources#Projects:_Getting_Started  

Incubation phase  
https://wiki.eclipse.org/Development_Resources/HOWTO/Incubation_Phase   

##### Eclipsepedia

https://wiki.eclipse.org/Main_Page  

FAQs  
https://wiki.eclipse.org/The_Official_Eclipse_FAQs  

IP stuff   
https://wiki.eclipse.org/IP_Stuff     

##### Misc

Legal: committer “due diligence” guidelines  
https://www.eclipse.org/legal/committerguidelines.php

Project management infrastructure  
https://wiki.eclipse.org/Project_Management_Infrastructure 

Forges (community of communities)  
https://wiki.eclipse.org/Forges 

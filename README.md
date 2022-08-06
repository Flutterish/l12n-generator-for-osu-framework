# l12n generator for osu framework
 A localisation generator that works from json files to allow for easy community contributions

# For developers
There are 2 (recommended) methods to go about setting up l12n for your project:
## In the main repository
* Create a "Localisation" folder in the project files
* Run the program, and create a new project:
  * Select [Somewhere else] and pick the path to the "Localisation" folder
* [Link] the .csproj
* Click [Okay]
* Store the .json files [In the project files] or [Somewhere else] within the repository

Generating the .cs and .resx files is as easy as selecting [Generate .cs and .resx files],
however if a localisation contributor does this you need to make sure the generated files
aren't tampered with

## In a secondary repository
With this approach, contributors will not have to clone the whole codebase 
to contribute localisation files, but it takes slightly more effort to
generate the final .cs and .resx files

* Create a new repository where you will store localisation files
* Run the program, and create a new project:
  * Select [Somewhere else] and pick the path to the repository
* Select [Local]

To generate .cs and .resx files can't really [Link a .csproj file] because that would require
crossing the file boundary between 2 repositories.
Instead, each person that wants to generate the .cs and .resx files will need to create a new project
outside both repositories like this:
* Run the program and create a new project:
  * Select [Somewhere else] and pick wherever (outside both repositories)
* [Link] the .csproj (main repository)
* Click [Okay]
* Select [Somewhere else] and pick the location in the second repository where the .json files are

After this setup you can [Generate .cs and .resx files]

---

After generating the .cs and .resx files and compiling, the localisation files will be emmited as satellite libraries in folders named after the iso code of a given language. You might want to either bundle them all together, or offer as additional downloadable file each. The default locale is used as a fallback and is embedded into the .cs files - a satellite library is not generated for it

# For contributors
This is a simple tutorial you might want to include to explain how to contribute locale files:

There are 2 programs you will need to translate this project:
* Git (if you know how to use it) or Github Desktop
* [o!f-l12n (Localisation generator for osu!framework)](https://github.com/Flutterish/l12n-for-osu-framework/releases)

First, fork and clone this repository.
Forking creates a copy independent of this repository that we will edit and later merge back into this repository. Cloning does the same, but to your machine rather than on github.

![image](https://user-images.githubusercontent.com/40297338/183265969-c6e45dd0-8709-411f-822c-1923351e511b.png)

![image](https://user-images.githubusercontent.com/40297338/183265674-b4a434e1-e9e9-4327-a27f-a4b691d85dfc.png)

Open the folder you cloned it to (Repository -> Show in Explorer) and find a file called `l12nConfig.json`.
Generally it will be in a `Localisation` folder

Run the [o!f-l12n](https://github.com/Flutterish/l12n-for-osu-framework/releases) program 
and select [Select path] - choose the path to the folder containing the `l12nConfig.json` file

Everything else will already be set up for you by the developer, you can now
simply add or edit a locale.

Before finishing run the [Summary] option to see if there are any issues with the locales you edited.

Add a commit message, click Commit and Push.
Commiting creates a list of changes you made, and pushing sends that to your fork on github.
You can view all the changes ever made to the repository in the `History` tab

![image](https://user-images.githubusercontent.com/40297338/183266057-b9faf5e6-8764-4c61-aec4-cfc8ed4fccb7.png)

![image](https://user-images.githubusercontent.com/40297338/183266334-7186b743-0377-4b7c-b9c3-0d0506cd798f.png)

Now you can create a pull request, so your changes can be merged into this repository.
A pull request is the same thing as pushing from one repository to another, but with authorization from
the owner of the latter. We need to do this because you're not the owner of this repository, or just to verify that everything is okay with the changes you made

![image](https://user-images.githubusercontent.com/40297338/183265928-e1cd84e0-9ac3-4ec9-b5d7-47786120c563.png)

If you want to contribute again after that, you might find that the fork of the repository
you made is outdated. To fix this, you will need to open the fork on github and click "Sync fork"

![image](https://user-images.githubusercontent.com/40297338/183266241-6c0321c4-65ef-42ca-8a2f-35a7bf63e34b.png)

Then, you will need to Fetch (if it hasn't happened automatically) and Pull.
Fetching is simply checking if there are any changes made to the main repository (your fork),
while pulling is downloading them

![image](https://user-images.githubusercontent.com/40297338/183266309-a0060528-4666-4dd5-a0dc-29e081d00899.png)

![image](https://user-images.githubusercontent.com/40297338/183266319-ed26dad5-2967-4ed8-966d-f0fd6093645b.png)
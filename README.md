# GoFaster
GF (GoFaster) is a command line tool for daily work to sync code from perforce, open, build projects, etc. It's would be helpful in daily work and improve productivity.

# Branch Description
* dev: dev branch
* master: main branch
* admin: similar with master, but the GoFaster got require Administrator as execution level.

# What problems GoFaster resolves
**Save time to working with your projects, find helpful information, especially for new comers!**
* list all the projects we have and easily for developer to find by full name, partial name or number
* sync the code from Perforce, really helpful when the projects inside the solution came from different folders
* use `hosts set <options>` command to switch hosts file right away according to working projects

# Get Started
After you download GF (GoFaster), before you run the GoFaster tool, you need to make some configurations in gf.exe.config file:
1. **IMPORTANT:** If you got Perforce as your SCM. Please set up Perforce user name, client, local workspace location. It's required if you want to use `sync` command to sync the code for given project.
``` xml
    <add key="P4PORT" value="perforce:5050" />
    <add key="P4USER" value="slin" />
    <add key="P4Client" value="slin" />
    <!-- P4Workspace : is used to replace the workspace in path in profile.xml-->
    <add key="P4Workspace" value="C:\P4\" />
```
2. Set default working branch name and all available branches

    2.1. **IMPORTANT:** about working branch, some comany may use "current", some may use "dev" or "development", please set it right according your case.
	```
		<!-- DefaultWorkingBranch -->
		<add key="DefaultWorkingBranch" value="current" />
	```

    2.2. **IMPORTANT:** Set `BranchRegexPattern` which will be used to replace the path for your projects/solution file.
	``` xml
		<!-- Very Important: -->
		<add key="BranchRegexPattern" value="\\(current|offcycle|integration|production|trunk|release)\\" />
	```

3. Set hosts repositories
We commonly use server name for different services, and we use alias for server name, so the name would be same for different environments(DEV, INTEGRATION, QA, UAT, PRODUCTION). So hosts varies by environment. 
If you are not the case, please ignore this part. 

	**Dependence:** Perforce.
	``` xml
		<!--hosts repositories: name:p4path:environment-names-with-comma-->
		<add key="HostsRepositories" value="
			 default:\\SCM\Configs\HOSTS\:DEV-INT,DEV-INT2,QA-2,QA-3,QA-4,QA-5;
			 repo1:\\REPO1\Utils\hosts\:DEV,INT,QA,UAT,PROD;
			 repo2:\\REPO2\Utils\hosts\:DEV,INT,QA,STG;
			 repo3:\\REPO3\Utils\hosts\:DEV,INT,QA,STG;" />
	```
4. Add your projects into profile.xml

# Features
## List projects
List the projects with your given options: team, project name, category. the name can be full name or partial name. So it will be really convenient for you to find projects if there are too many projects in your teams.

**Examples**
	``` bash
	> ls

	> ls team:name-of-team name:name-of-project category:name-of-category

	# NOTE: name can be partial name
	> ls team:^start-part-name-of-team  name:end-part-name-of-project$

	> ls team:all

	--short command pattern
	> ls name-or-number-of-project
	```

    You can set your default teams in gf.exe.config file, that only the projects owned by the teams will be listed.

<details>
    <summary>Click to expanddemonstration video for `list` (or `ls`) command</summary>
    <img src="https://github.com/sw0/GoFaster/blob/dev/manual-gifs/gf-001-ls.gif?raw=true"/>
</details>

## Sync code from Perforce
Synchronize the code from source control server, here it's Perforce.

**Example**
	``` bash
	> sync 1   #sync the code for project with number 1

	> sync HelloWorld  b:int  # sync the code for 'integration' branch for project with name contains "helloworld"

	> sync ^Hello --force #force sync the code for project with name starts with 'Hello'

	> sync orld$ b:integration --force  #force sync the integration branch code for project with name ends with 'orld'

	> sync ell b:offcycle

	> sync ell b:off --force
	```

<details>
    <summary>Click to expand: demonstration video for `sync` command</summary>
    <img src="https://github.com/sw0/GoFaster/blob/dev/manual-gifs/gf-002-sync.gif?raw=true"/>
</details>


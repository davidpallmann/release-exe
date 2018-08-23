# release-exe
release.exe is a Windows command for generating full / differential release manifests &amp; verifying them

Quick command reference:

<b>release create</b> <i>release-name</i>.txt</br>
creates a full release manifest file.

<b>release verify</b> <i>release-name</i>.txt</br>
verifies a release manifest file.

<b>release diff</b> release-name.txt <i>prior-release-name</i>.txt</br>
creates a differential release (removes unchanged files).

<b>release hash</b> <i>file</i></br>
diplays a hash for the specified file.

For a walk-through of how to use release.exe, see this blog post:
https://davidpallmann.blogspot.com/2018/08/release-management-and-my-release-tool.html

Get-ChildItem "Source" |
Foreach-Object {
	$filename = $_.Basename
	glslc $_.FullName -o Compiled/$filename.spv
}

cdata inputs ->always in datasegment even if not used
cdata defines -> only inlined in code if referneced (refcount > 0)
-  packager should set vectorsize to the nr of 4bytes elements on cdata variables encoded in datasegment

- new ops: size, cdata, cdataref, alert
- new datatype: data arrays|blobs as variable input
- new define: #cdata name data, defines constantdata  

**V1.0.2** 2022-04-02

- all & any support for bool32 
- stricter datatype verification on parameters in variable parametercount functions
- helper functions for setting up datasegments with default data (package.SetDefaultData)

**V1.0.1** 2022-04-01

- bugfixes for release build defines
- bugfix in node.getothernodes
- bool32 structure
- sample 11 - set | get bit


**V1.0.0** 2022-03-29 - Initial Release

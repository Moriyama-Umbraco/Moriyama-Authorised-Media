Moriyama Authorised Media

Back office login
-----------------

darren@moriyama.co.uk
abc123abc123

Overview
--------

For Umbraco 7.7+ (not v8) - Will protected media in folders based on membergroups.

Create a media type with an alias "protectedFolder" and a property memberGroups which is comma delimited list of membergroups. A member must be in one of these groups to access items in the folder.

Needs the following supporting database table:

create table AuthorisedMedia(
MediaPathId int not null,
MediaParentId int not null
)

CREATE UNIQUE INDEX AuthorisedMediaIdx
ON AuthorisedMedia(MediaPathId, MediaParentId);

How it works
------------

On media save, an entry is recorded in the database for the media item if it has a parent media type "protectedFolder" - this is also removed on save, should the item be moved out of a protected folder.

An http module checks inbound requests for paths mathing /media/{folderId}/filename

If matched, it will check the database table to see if there is protection on the item. for performance non protected items are bypassed for a minute - which avoids too many database queries.

Restrictions
------------

Recursion of protected folder properties does not occur down the tree. 

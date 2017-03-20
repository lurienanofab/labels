$.nametags = {};

$.nametags.MANAGER_OBJECT_NAME = "HEADING3";
$.nametags.STAFF_OBJECT_TEXT = "LNF Staff";
$.nametags.INTERN_OBJECT_TEXT = "LNF Intern";
$.nametags.LNAME_OBJECT_NAME = "LastName";
$.nametags.FNAME_OBJECT_NAME = "FirstName";
$.nametags.STARTDATE_OBJECT_NAME = "StartDate";

$.nametags.LabelData = function () {
    this.user = { lname: null, fname: null, start: null };
    this.manager = { lname: null, fname: null, start: null };
    this.org = { name: null, internal: false };
    this.staff = false;
    this.intern = false;

    this.applyTo = function (label) {
        label.setObjectText($.nametags.LNAME_OBJECT_NAME, this.user.lname);
        label.setObjectText($.nametags.FNAME_OBJECT_NAME, this.user.fname);

        var objectNames = label.getObjectNames();
        var hasManagerObject = $.inArray($.nametags.MANAGER_OBJECT_NAME, objectNames) !== -1;

        if (hasManagerObject) {
            if (this.intern) {
                label.setObjectText($.nametags.MANAGER_OBJECT_NAME, $.nametags.INTERN_OBJECT_TEXT);
                label.setObjectText($.nametags.STARTDATE_OBJECT_NAME, " ");
            } else if (this.staff) {
                label.setObjectText($.nametags.MANAGER_OBJECT_NAME, $.nametags.STAFF_OBJECT_TEXT);
                label.setObjectText($.nametags.STARTDATE_OBJECT_NAME, " ");
            } else {
                if (this.org.internal) {
                    label.setObjectText($.nametags.MANAGER_OBJECT_NAME, "Prof. " + this.manager.lname);
                } else {
                    label.setObjectText($.nametags.MANAGER_OBJECT_NAME, this.org.name);
                }

                label.setObjectText($.nametags.STARTDATE_OBJECT_NAME, this.user.start);
            }
        }
    }
}

$.fn.nametags = function () {
    return this.each(function (options) {
        var $this = $(this);

        $.nametags.LabelData.create = function () {
            var result = new $.nametags.LabelData();

            var userOpt = $(".users", $this).find("option:selected");

            if (userOpt) {
                $.extend(result.user, userOpt.data());
            }

            var managerOpt = $(".managers", $this).find("option:selected");

            if (managerOpt) {
                $.extend(result.manager, managerOpt.data());
            }

            var orgOpt = $(".orgs", $this).find("option:selected");

            if (orgOpt) {
                $.extend(result.org, orgOpt.data());
            }

            result.staff = $(".staff", $this).prop("checked");
            result.intern = $(".intern", $this).prop("checked");

            return result;
        }

        var labels = {};

        var opts = $.extend({}, { "apiurl": "api/", "labelurl": "labels/" }, options, $this.data());

        // dymo sdk is required
        if (!dymo) {
            alert('DYMO Label Framework not found!');
            return
        }

        dymo.label.framework.getPrintersAsync().then(function (printers) {
            if (printers.length > 0) {
                $(".dymo-printers", $this).html($.map(printers, function (item, index) {
                    return $("<option/>").text(item.name);
                }));
            } else {
                $(".dymo-printers", $this).html($("<option/>").text("No printers found.")).prop("disabled", true);
            }
        });


        var getDisplayName = function (user) {
            return user.LName + ", " + user.FName;
        }

        var loadUsers = function () {
            $.ajax({
                "url": opts.apiurl + "user",
                "method": "GET"
            }).done(function (data, textStatus, xhr) {
                $(".users", $this)
                    .html($("<option/>", { "value": "0" }).text("-- Select --"))
                    .append($.map(data, function (item, index) {
                        return $("<option/>", { "value": item.ClientID, "data-lname": item.LName, "data-fname": item.FName, "data-start": moment(item.StartDate).format("MMM YYYY") }).text(getDisplayName(item));
                    }));
            });
        }

        var loadManagers = function (clientId) {
            $.ajax({
                "url": opts.apiurl + "user/" + clientId + "/manager",
                "method": "GET"
            }).done(function (data, textStatus, xhr) {
                $(".managers", $this)
                    .html($("<option/>", { "value": "0" }).text("-- Select --"))
                    .append($.map(data, function (item, index) {
                        return $("<option/>", { "value": item.ClientID, "data-lname": item.LName, "data-fname": item.FName, "data-start": moment(item.StartDate).format("MMM YYYY") }).text(getDisplayName(item));
                    }));

                $(".orgs", $this).html("");
            });
        }

        var loadManagerOrgs = function (clientId, managerId) {
            var def = $.Deferred();

            $.ajax({
                "url": opts.apiurl + "user/" + clientId + "/manager/" + managerId + "/org",
                "method": "GET"
            }).done(function (data, textStatus, xhr) {
                $(".orgs", $this)
                    .html($.map(data, function (item, index) {
                        return $("<option/>", { "value": item.OrgID, "data-name": item.OrgName, "data-internal": item.Internal }).text(item.OrgName);
                    }));
                def.resolve();
            }).fail(def.reject);

            return def.promise();
        }

        var getLabel = function (file, labelData) {
            var def = $.Deferred();

            var labelUrl = opts.labelurl + file + ".xml";

            var label = labels[file];

            if (label) {
                labelData.applyTo(label);
                def.resolve(label);
            } else {
                $.ajax({
                    "url": labelUrl,
                    "method": "GET",
                    "dataType": "text"
                }).done(function (data) {
                    label = dymo.label.framework.openLabelXml(data);
                    labelData.applyTo(label);
                    labels[file] = label;
                    def.resolve(label);
                }).fail(def.reject);
            }

            return def.promise();
        }

        var showPreview = function (file, labelData) {
            getLabel(file, labelData).done(function (label) {
                label.renderAsync('<LabelRenderParams><ShadowColor Alpha="230" Red="128" Green="128" Blue="128"/></LabelRenderParams>').then(function (pngData) {
                    $(".preview-panel[data-file='" + file + "'] .label-preview", $this).html($("<img/>", { "src": "data:image/png;base64," + pngData }));
                }).thenCatch(function (err) {
                    console.log(err);
                });
            });
        }

        var showCustomPreview = function (top, bottom) {
            var labelData = new $.nametags.LabelData();

            labelData.user = { fname: top, lname: " ", start: bottom };
            labelData.manager = { fname: " ", lname: " ", start: null };
            labelData.org = { name: " ", internal: false };

            showPreview("userlabel", labelData);
        }

        var clearPreview = function () {
            $(".label-preview", $this).html("");
        }

        var clearSelections = function () {
            $(".users", $this)[0].selectedIndex = 0;
            $(".managers", $this).html("");
            $(".orgs", $this).html("");
        }

        loadUsers();

        $this.on("change", ".users", function (e) {
            var clientId = $(this).val();

            if (clientId > 0) {
                loadManagers(clientId);
            } else {
                $(".managers", $this).html("");
                $(".orgs", $this).html("");
                $(".label-preview", $this).html("");
            }
        }).on("change", ".managers", function (e) {
            var clientId = $(".users", $this).val();
            var managerId = $(this).val();

            if (clientId > 0 && managerId > 0) {
                loadManagerOrgs(clientId, managerId).done(function () {
                    var labelData = $.nametags.LabelData.create();
                    $this.trigger("show-preview", labelData);
                });
            } else {
                $(".orgs", $this).html("");
                clearPreview();
            }
        }).on("change", ".orgs", function (e) {
            var clientId = $(".users", $this).val();
            var managerId = $(".managers", $this).val();

            if (clientId > 0 && managerId > 0) {
                var labelData = $.nametags.LabelData.create();
                $this.trigger("show-preview", labelData);
            }
        }).on("change", ".staff, .intern", function (e) {
            var clientId = $(".users", $this).val();
            var managerId = $(".managers", $this).val();

            if (clientId > 0 && managerId > 0) {
                var labelData = $.nametags.LabelData.create();
                $this.trigger("show-preview", labelData);
            }
        }).on("change", ".custom", function (e) {
            clearPreview();

            if ($(this).prop("checked")) {
                $(".preview-panel[data-file='mini']", $this).hide();
                $(".standard-label", $this).hide();
                $(".custom-label", $this).show();

                $(".top-text", $this).val("Visitor");
                $(".bottom-text", $this).val("Medium");

                showCustomPreview($(".top-text", $this).val(), $(".bottom-text", $this).val());
            } else {
                $(".preview-panel[data-file='mini']", $this).show();
                $(".custom-label", $this).hide();
                $(".standard-label", $this).show();

                var clientId = $(".users", $this).val();
                var managerId = $(".managers", $this).val();

                if (clientId > 0 && managerId > 0) {
                    var labelData = $.nametags.LabelData.create();
                    $this.trigger("show-preview", labelData);
                }
            }
        }).on("change", ".top-text, .bottom-text", function (e) {
            $this.trigger("show-custom-preview", [$(".top-text", $this).val(), $(".bottom-text", $this).val()])
        }).on("show-preview", function (e, labelData) {
            showPreview("userlabel", labelData);
            showPreview("mini", labelData);
        }).on("click", ".print-label", function (e) {
            var file = $(this).closest(".preview-panel").data("file");
            var printer = $(".dymo-printers option:selected", $this).text();

            var label = labels[file];

            if (label) {
                var pp = null;

                if (file == "mini")
                    pp = { twinTurboRoll: dymo.label.framework.TwinTurboRoll.Right };
                else
                    pp = { twinTurboRoll: dymo.label.framework.TwinTurboRoll.Left };

                var printParams = dymo.label.framework.createLabelWriterPrintParamsXml(pp);

                label.printAsync(printer, printParams);
            }
        }).on("show-custom-preview", function (e, top, bottom) {
            showCustomPreview(top, bottom);
        });
    });
}
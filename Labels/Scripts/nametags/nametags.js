function LabelData() {
    var MANAGER_OBJECT_NAME = "HEADING3";
    var STAFF_OBJECT_TEXT = "LNF Staff";
    var INTERN_OBJECT_TEXT = "LNF Intern";
    var LNAME_OBJECT_NAME = "LastName";
    var FNAME_OBJECT_NAME = "FirstName";
    var STARTDATE_OBJECT_NAME = "StartDate";

    this.user = { lname: null, fname: null, start: null };
    this.manager = { lname: null, fname: null, start: null };
    this.org = { name: null, internal: false };
    this.staff = false;
    this.intern = false;

    this.applyTo = function (label) {
        label.setObjectText(LNAME_OBJECT_NAME, this.user.lname);
        label.setObjectText(FNAME_OBJECT_NAME, this.user.fname);

        var objectNames = label.getObjectNames();
        var hasManagerObject = $.inArray(MANAGER_OBJECT_NAME, objectNames) !== -1;

        if (hasManagerObject) {
            if (this.intern) {
                label.setObjectText(MANAGER_OBJECT_NAME, INTERN_OBJECT_TEXT);
                label.setObjectText(STARTDATE_OBJECT_NAME, " ");
            } else if (this.staff) {
                label.setObjectText(MANAGER_OBJECT_NAME, STAFF_OBJECT_TEXT);
                label.setObjectText(STARTDATE_OBJECT_NAME, " ");
            } else {
                if (this.org.internal && this.manager.lname !== " ") {
                    label.setObjectText(MANAGER_OBJECT_NAME, "Prof. " + this.manager.lname);
                } else {
                    label.setObjectText(MANAGER_OBJECT_NAME, this.org.name);
                }

                label.setObjectText(STARTDATE_OBJECT_NAME, this.user.start);
            }
        }
    }
}

$.fn.nametags = function () {
    return this.each(function (options) {
        var $this = $(this);

        LabelData.create = function () {
            var result = new LabelData();

            var userOpt = $(".users", $this).find("option:selected");

            if (userOpt) {
                $.extend(result.user, userOpt.data());
            }

            if ($(".managers", $this).val() == 0) {
                result.manager.fname = " ";
                result.manager.lname = " ";
                result.manager.start = " ";
            } else {
                var managerOpt = $(".managers", $this).find("option:selected");
                if (managerOpt) {
                    $.extend(result.manager, managerOpt.data());
                }
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

        var printParams = {
            "mini": { twinTurboRoll: dymo.label.framework.TwinTurboRoll.Right },
            "userlabel": { twinTurboRoll: dymo.label.framework.TwinTurboRoll.Left }
        };

        var opts = $.extend({}, { "apiurl": "api/", "labelurl": "labels/" }, options, $this.data());

        var getDisplayName = function (user) {
            return user.LName + ", " + user.FName;
        }

        var loadUsers = function () {
            var def = $.Deferred();

            $.ajax({
                "url": opts.apiurl + "user",
                "method": "GET"
            }).done(function (data, textStatus, xhr) {
                $(".users", $this).html(
                    $("<option/>", { "value": "0" }).text("-- Select --")
                ).append($.map(data, function (item, index) {
                    return $("<option/>", { "value": item.ClientID, "data-lname": item.LName, "data-fname": item.FName, "data-phone": item.Phone, "data-email": item.Email, "data-start": moment(item.StartDate).format("MMM YYYY") }).text(getDisplayName(item));
                }));

                def.resolve();
            }).fail(def.reject);

            return def.promise();
        }

        var loadManagers = function (clientId) {
            var def = $.Deferred();

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

                def.resolve();
            }).fail(def.reject);

            return def.promise();
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

        var loadClientOrgs = function (clientId) {
            var def = $.Deferred();

            $.ajax({
                "url": opts.apiurl + "user/" + clientId + "/org",
                "method": "GET"
            }).done(function (data, textStatus, xhr) {
                $(".orgs", $this)
                    .html($.map(data, function (item, index) {
                        return $("<option/>", { "value": item.OrgID, "data-name": item.OrgName, "data-internal": item.Internal }).text(item.OrgName);
                    }));
                def.resolve();
            });

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
            var preview = $(".preview-panel[data-file='" + file + "'] .label-preview", $this);

            preview.html($("<div/>", { "class": "loader" }));

            getLabel(file, labelData).done(function (label) {
                var renderParams = dymo.label.framework.createLabelRenderParamsXml({ shadowColor: { alpha: 0, red: 170, green: 170, blue: 170 }, shadowDepth: 50 });
                label.renderAsync(renderParams).then(function (pngData) {
                    preview.html($("<img/>", { "src": "data:image/png;base64," + pngData }));
                }).thenCatch(function (err) {
                    console.log(err);
                });
            });
        }

        var showCustomPreview = function (top, bottom) {
            var labelData = new LabelData();

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

        var loadPrinters = function () {
            var def = $.Deferred();

            // dymo sdk is required
            if (!dymo) {
                alert('DYMO Label Framework not found!');
                def.reject();
            } else {

                dymo.label.framework.getPrintersAsync().then(function (printers) {
                    if (printers.length > 0) {
                        $(".dymo-printers", $this).html($.map(printers, function (item, index) {
                            return $("<option/>").text(item.name);
                        }));

                        def.resolve();
                    } else {
                        $(".dymo-printers", $this).html($("<option/>").text("No printers found.")).prop("disabled", true);
                        def.reject();
                    }
                }).thenCatch(def.reject);
            }

            return def.promise();
        }

        loadPrinters().done(function () {
            loadUsers();
        });

        $this.on("change", ".users", function (e) {
            var clientId = $(this).val();

            if (clientId > 0) {
                loadManagers(clientId).done(function () {
                    loadClientOrgs(clientId).done(function () {
                        var labelData = LabelData.create();
                        $this.trigger("show-preview", labelData)
                    });
                });
            } else {
                $(".managers", $this).html("");
                $(".orgs", $this).html("");
                $(".label-preview", $this).html("");
                clearPreview();
            }
        }).on("change", ".managers", function (e) {
            var clientId = $(".users", $this).val();
            var managerId = $(this).val();

            if (clientId > 0) {
                if (managerId > 0) {
                    loadManagerOrgs(clientId, managerId).done(function () {
                        var labelData = LabelData.create();
                        $this.trigger("show-preview", labelData);
                    });
                } else {
                    loadClientOrgs(clientId).done(function () {
                        var labelData = LabelData.create();
                        $this.trigger("show-preview", labelData);
                    });
                }
            } else {
                $(".orgs", $this).html("");
                clearPreview();
            }
        }).on("change", ".orgs", function (e) {
            var clientId = $(".users", $this).val();
            var managerId = $(".managers", $this).val();

            if (clientId > 0) {
                var labelData = LabelData.create();
                $this.trigger("show-preview", labelData);
            } else {
                clearPreview();
                clearSelections();
            }
        }).on("change", ".staff, .intern", function (e) {
            var clientId = $(".users", $this).val();
            var managerId = $(".managers", $this).val();

            if (clientId > 0 && managerId > 0) {
                var labelData = LabelData.create();
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
                    var labelData = LabelData.create();
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
                var pp = dymo.label.framework.createLabelWriterPrintParamsXml(printParams[file]);
                label.printAsync(printer, pp);
            }
        }).on("show-custom-preview", function (e, top, bottom) {
            showCustomPreview(top, bottom);
        });
    });
}